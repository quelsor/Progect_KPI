
public class AmazonModel
{
    public string title { get; set; }
    public Product_Information product_information { get; set; }
    public string brand { get; set; }
    public string brand_url { get; set; }
    public string description { get; set; }
    public string price { get; set; }
    public string list_price { get; set; }
    public string shipping_info { get; set; }
    public string availability_status { get; set; }
    public string[] images { get; set; }
    public int number_of_videos { get; set; }
    public string product_category { get; set; }
    public double average_rating { get; set; }
    public string[] feature_bullets { get; set; }
    public int total_reviews { get; set; }
    public string total_answered_questions { get; set; }
    public object[] other_sellers { get; set; }
    public Customization_Options customization_options { get; set; }
    public string merchant_info { get; set; }
    public string ships_from { get; set; }
    public string sold_by { get; set; }
}

public class Product_Information
{
    public string UPCundefined { get; set; }
    public string ASINundefined { get; set; }
    public string CustomerReviews { get; set; }
}

public class Customization_Options
{
    public object[] color { get; set; }
    public object[] size { get; set; }
    public object[] style { get; set; }
}
