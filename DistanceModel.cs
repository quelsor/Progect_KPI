namespace Progect.Models
{
    public class DistanceModel
    {
        public float distance { get; set; }
        public string unit { get; set; }
        public Location1 location1 { get; set; }
        public Location2 location2 { get; set; }
    }

    public class Location1
    {
        public int place_id { get; set; }
        public string licence { get; set; }
        public string osm_type { get; set; }
        public int osm_id { get; set; }
        public string _class { get; set; }
        public string type { get; set; }
        public int place_rank { get; set; }
        public float importance { get; set; }
        public string addresstype { get; set; }
        public string name { get; set; }
        public string display_name { get; set; }
        public Address address { get; set; }
        public string[] boundingbox { get; set; }
    }

    public class Address
    {
        public string city { get; set; }
        public string ISO31662lvl6 { get; set; }
        public string state { get; set; }
        public string ISO31662lvl4 { get; set; }
        public string region { get; set; }
        public string country { get; set; }
        public string country_code { get; set; }
    }

    public class Location2
    {
        public int place_id { get; set; }
        public string licence { get; set; }
        public string osm_type { get; set; }
        public int osm_id { get; set; }
        public string lat { get; set; }
        public string lon { get; set; }
        public string _class { get; set; }
        public string type { get; set; }
        public int place_rank { get; set; }
        public float importance { get; set; }
        public string addresstype { get; set; }
        public string name { get; set; }
        public string display_name { get; set; }
        public Address1 address { get; set; }
        public string[] boundingbox { get; set; }
    }

    public class Address1
    {
        public string city { get; set; }
        public string state { get; set; }
        public string ISO31662lvl4 { get; set; }
        public string country { get; set; }
        public string country_code { get; set; }
    }

}

