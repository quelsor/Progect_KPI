using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Logging;
using Project.Controllers;
using Progect.Models;
using MongoExamples.Services;
using Microsoft.Extensions.Options;
using MongoExamples.Models;

namespace TelegramBot
{
    public class TelegramBotTest
    {
        private readonly ILogger<ModelController> _logger;
        private readonly TelegramBotClient client;
        private readonly CancellationToken cancellationToken;
        private readonly ReceiverOptions receiverOptions;
        private readonly ConcurrentDictionary<long, string> userStates;
        private readonly ConcurrentDictionary<long, List<string>> userCarts;
        private readonly ConcurrentDictionary<long, DeliveryModel> userDeliveries;
        private readonly ModelController modelController;
        private readonly MongoDBService _mongoDBService;

        public TelegramBotTest(ILogger<ModelController> logger)
        {
            _logger = logger;
            client = new TelegramBotClient("7180436970:AAEnfQKvxe5LI4MTRCkc9XQP-Ob4RpOvlb4");
            cancellationToken = new CancellationToken();
            receiverOptions = new ReceiverOptions { AllowedUpdates = { } };
            userStates = new ConcurrentDictionary<long, string>();
            userCarts = new ConcurrentDictionary<long, List<string>>();
            userDeliveries = new ConcurrentDictionary<long, DeliveryModel>();
            modelController = new ModelController(_logger);
            _mongoDBService = new MongoDBService(new OptionsWrapper<MongoDBSettings>(new MongoDBSettings()));
        }

        public async Task Start()
        {
            client.StartReceiving(HandlerUpdateAsync, HandlerError, receiverOptions, cancellationToken);
            var botMe = await client.GetMeAsync();
            Console.WriteLine($"Bot {botMe.Username} started working");
        }

        private Task HandlerError(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Error in TgBot Api:\n {apiRequestException.ErrorCode}" +
                                                           $"\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }

        private async Task HandlerUpdateAsync(ITelegramBotClient client, Update update, CancellationToken token)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Text != null)
            {
                await HandlerMessageAsync(client, update.Message);
            }
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                await HandlerCallbackQueryAsync(client, update.CallbackQuery);
            }
        }

        private async Task HandlerMessageAsync(ITelegramBotClient client, Message message)
        {
            var chatId = message.Chat.Id;

            if (message.Text == "/start")
            {
                await client.SendTextMessageAsync(chatId, 
    "Welcome to the Shopping Assistant Bot! 🛒\n\n" +
    "Here's how I can help you:\n" +
    "- Use /keyboard to open the main menu.\n" +
    "- 'Search new Item' to find products by ID.\n" +
    "- 'Item Description' to get details about the product you've searched for.\n" +
    "- 'View Cart' to see items you've added to your cart.\n" +
    "- 'Create a Delivery' to arrange a delivery for your items.\n\n" +
    "Let's get started! Use the /keyboard command to begin.");
            }
            else if (message.Text == "/keyboard")
            {
                ReplyKeyboardMarkup replyKeyboardMarkup = new
                (
                    new[]
                    {
                        new KeyboardButton[] { "Search new Item", "Item Description" },
                        new KeyboardButton[] { "View Cart", "Create a Delivery" }
                    }
                )
                {
                    ResizeKeyboard = true
                };
                await client.SendTextMessageAsync(chatId, "Choose menu item:", replyMarkup: replyKeyboardMarkup);
            }
            else if (message.Text == "Search new Item")
            {
                await client.SendTextMessageAsync(chatId, "Please, write your product id:");
                userStates[chatId] = "awaiting_product_id";
            }
            else if (userStates.TryGetValue(chatId, out string state) && state == "awaiting_product_id")
            {
                userStates.TryRemove(chatId, out _);
                string id = message.Text;
                string itemInfo = await SearchItem(id);
                userStates[chatId] = id;

                InlineKeyboardMarkup inlineKeyboardMarkup = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Add to cart", "AddToCart")
                    }
                });

                await client.SendTextMessageAsync(chatId, $"Item Info:\n {itemInfo}", replyMarkup: inlineKeyboardMarkup);
            }
            else if (message.Text == "Item Description")
            {
                if (userStates.TryGetValue(chatId, out string id))
                {
                    string itemDescription = await ItemDescription(id);
                    await client.SendTextMessageAsync(chatId, $"Item Description:\n {itemDescription}");
                }
                else
                {
                    await client.SendTextMessageAsync(chatId, "Please search for an item first.");
                }
            }
            else if (message.Text == "View Cart")
            {
                await HandleViewCart(chatId);
            }
            else if (message.Text == "Create a Delivery")
            {
                await StartDeliveryProcess(chatId);
            }
            else if (userStates.TryGetValue(chatId, out state))
            {
                await ProcessDeliveryData(chatId, message.Text, state);
            }
            else
            {
                await client.SendTextMessageAsync(chatId, "Unknown command");
            }
        }

        private async Task HandlerCallbackQueryAsync(ITelegramBotClient client, CallbackQuery callbackQuery)
        {
            long chatId = callbackQuery.Message.Chat.Id;
            string data = callbackQuery.Data;

            if (data == "AddToCart")
            {
                await HandleAddToCart(chatId);
            }
            else if (data.StartsWith("Remove_"))
            {
                if (int.TryParse(data.Split('_')[1], out int itemIndex))
                {
                    await HandleRemoveFromCart(chatId, itemIndex);
                }
            }
            else if (data == "ConfirmDelivery")
            {
                await ConfirmDelivery(chatId);
            }
            else if (data == "CancelDelivery")
            {
                await StartDeliveryProcess(chatId);
            }

            await client.AnswerCallbackQueryAsync(callbackQuery.Id);
        }

        private async Task HandleAddToCart(long chatId)
        {
            if (userStates.TryGetValue(chatId, out string productId))
            {
                if (!userCarts.ContainsKey(chatId))
                {
                    userCarts[chatId] = new List<string>();
                }

                string productName = await modelController.ItemCart(productId);
                userCarts[chatId].Add(productName);
                await client.SendTextMessageAsync(chatId, $"Product {productName} added to your cart.");
            }
        }

        private async Task HandleViewCart(long chatId)
        {
            if (userCarts.TryGetValue(chatId, out List<string> cartItems) && cartItems.Count > 0)
            {
                string cartContents = "Your cart contains:\n" + string.Join("\n", cartItems);
                InlineKeyboardMarkup inlineKeyboardMarkup = new InlineKeyboardMarkup(cartItems.Select((item, index) =>
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData($"Remove {item}", $"Remove_{index}")
                    }));
                await client.SendTextMessageAsync(chatId, cartContents, replyMarkup: inlineKeyboardMarkup);
            }
            else
            {
                await client.SendTextMessageAsync(chatId, "Your cart is empty.");
            }
        }

        private async Task HandleRemoveFromCart(long chatId, int itemIndex)
        {
            if (userCarts.TryGetValue(chatId, out List<string> cartItems) && cartItems.Count > itemIndex)
            {
                string removedItem = cartItems[itemIndex];
                cartItems.RemoveAt(itemIndex);
                await client.SendTextMessageAsync(chatId, $"Removed {removedItem} from your cart.");
            }
            else
            {
                await client.SendTextMessageAsync(chatId, "Invalid item index.");
            }
        }

        private async Task<string> SearchItem(string id)
        {
            string itemName = await modelController.Item(id);
            return itemName;
        }

        private async Task<string> ItemDescription(string id)
        {
            string itemDescription = await modelController.ItemInfo(id);
            return itemDescription;
        }

        private async Task StartDeliveryProcess(long chatId)
        {
            userStates[chatId] = "awaiting_pickup_phone";
            userDeliveries[chatId] = new DeliveryModel();
            await client.SendTextMessageAsync(chatId, "Please enter the pickup phone number:");

        }



        private async Task ProcessDeliveryData(long chatId, string data, string state)
        {
            switch (state)
            {
                case "awaiting_pickup_phone":
                    userDeliveries[chatId].PickupPhoneNumber = data;
                    userStates[chatId] = "awaiting_pickup_name";
                    await client.SendTextMessageAsync(chatId, "Please enter the pickup name:");
                    break;

                case "awaiting_pickup_name":
                    userDeliveries[chatId].PickupName = data;
                    userStates[chatId] = "awaiting_weight";
                    await client.SendTextMessageAsync(chatId, "Please enter the weight:");
                    break;

                case "awaiting_weight":
                    if (double.TryParse(data, out double weight))
                    {
                        userDeliveries[chatId].Weight = weight;
                        userStates[chatId] = "awaiting_pickup_location";
                        await client.SendTextMessageAsync(chatId, "Please enter the pickup location:");
                    }
                    else
                    {
                        await client.SendTextMessageAsync(chatId, "Invalid weight format. Please try again.");
                    }
                    break;

                case "awaiting_pickup_location":
                    userDeliveries[chatId].PickupLocation = data;
                    userStates[chatId] = "awaiting_post_office";
                    await client.SendTextMessageAsync(chatId, "Please enter the post office location:");
                    break;

                case "awaiting_post_office":
                    userDeliveries[chatId].Post = data;
                    userDeliveries[chatId].DropoffLocation = "New York";
                    Random random = new Random();
                    double Idnumber = random.Next(1, 999999999);
                    userStates[chatId] = "awaiting_confirmation";
                    var deliveryInfo = userDeliveries[chatId];
                    string confirmationMessage = $"Please confirm the delivery details:\n" +
                                                 $"Pickup Phone: {deliveryInfo.PickupPhoneNumber}\n" +
                                                 $"Pickup Name: {deliveryInfo.PickupName}\n" +
                                                 $"Weight: {deliveryInfo.Weight}\n" +
                                                 $"Pickup Location: {deliveryInfo.PickupLocation}\n" +
                                                 $"Post Office: {deliveryInfo.Post}\n" +
                                                 $"Dropoff Location: {deliveryInfo.DropoffLocation}\n" +
                                                 $"Id number: {Idnumber}";

                    InlineKeyboardMarkup inlineKeyboardMarkup = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Yes", "ConfirmDelivery"),
                            InlineKeyboardButton.WithCallbackData("No", "CancelDelivery")
                        }
                    });

                    await client.SendTextMessageAsync(chatId, confirmationMessage, replyMarkup: inlineKeyboardMarkup);
                    break;
            }

        }

        private async Task ConfirmDelivery(long chatId)
        {
            await SaveUserDataToMongoDB(chatId);

            var deliveryInfo = userDeliveries[chatId];

            float distance = await modelController.Distance(deliveryInfo.PickupLocation, deliveryInfo.DropoffLocation);
            double deliveryCost = modelController.CalculateDeliveryCost(distance, deliveryInfo.Weight);

            double totalCost = deliveryCost;
            if (userCarts.TryGetValue(chatId, out List<string> cartItems))
            {
                Random random = new Random();
                double cartTotal = cartItems.Count * random.Next(1, 101);
                totalCost += cartTotal;

                string cartContents = string.Join("\n", cartItems);
                await client.SendTextMessageAsync(chatId, $"Cart items:\n{cartContents}");
            }

            string confirmationMessage = $"Distance: {distance} km\n" +
                                         $"Delivery Cost: ${deliveryCost}\n" +
                                         $"Total Cost: ${totalCost}\n" +
                                         $"Order received! Your delivery will arrive in 14-20 days. ❤️";


            await client.SendTextMessageAsync(chatId, confirmationMessage);
        }

        private async Task SaveUserDataToMongoDB(long chatId)
        {
            if (!userDeliveries.ContainsKey(chatId))
            {
                Console.WriteLine($"Error: No delivery data found for chat ID {chatId}");
                return;
            }

            var deliveryInfo = userDeliveries[chatId];

            if (string.IsNullOrEmpty(deliveryInfo.PickupPhoneNumber) || string.IsNullOrEmpty(deliveryInfo.PickupName) || string.IsNullOrEmpty(deliveryInfo.PickupLocation) || string.IsNullOrEmpty(deliveryInfo.Post))
            {
                Console.WriteLine($"Error: Pickup phone number, name, location, or post not found for chat ID {chatId}");
                return;
            }

            string cartId = GenerateRandomCartId();

            List<string> selectedItems = userCarts.TryGetValue(chatId, out var items) ? items : new List<string>();

            Progect.Models.User user = new Progect.Models.User
            {
                UserName = deliveryInfo.PickupName,
                CartId = cartId,
                SelectedItems = selectedItems,
                PickupLocation = deliveryInfo.PickupLocation,
                Post = deliveryInfo.Post
            };

            await _mongoDBService.CreateAsync(user);
        }

        private string GenerateRandomCartId()
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 10)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }


        private async Task<List<string>> GetSelectedItems(List<string> itemIds)
        {
            var selectedItems = new List<string>();

            foreach (var itemId in itemIds)
            {
                string itemInfo = await modelController.ItemInfo(itemId);
                selectedItems.Add(itemInfo);
            }

            return selectedItems;
        }

    }

    public class Program
    {
        public static async Task Main(string[] args)
        {
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            ILogger<ModelController> logger = loggerFactory.CreateLogger<ModelController>();

            TelegramBotTest bot = new TelegramBotTest(logger);
            await bot.Start();

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
