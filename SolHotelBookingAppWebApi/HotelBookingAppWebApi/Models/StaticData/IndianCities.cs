using HotelBookingAppWebApi.Models.DTOs.City;

namespace HotelBookingAppWebApi.Models.StaticData
{
    public static class IndianCities
    {
        private static readonly IReadOnlyList<IndianCityDto> _cities = new List<IndianCityDto>
        {
            new() { CityName = "Mumbai",          StateName = "Maharashtra",      SamplePin = "400001" },
            new() { CityName = "Delhi",           StateName = "Delhi",            SamplePin = "110001" },
            new() { CityName = "Bengaluru",       StateName = "Karnataka",        SamplePin = "560001" },
            new() { CityName = "Hyderabad",       StateName = "Telangana",        SamplePin = "500001" },
            new() { CityName = "Chennai",         StateName = "Tamil Nadu",       SamplePin = "600001" },
            new() { CityName = "Kolkata",         StateName = "West Bengal",      SamplePin = "700001" },
            new() { CityName = "Pune",            StateName = "Maharashtra",      SamplePin = "411001" },
            new() { CityName = "Ahmedabad",       StateName = "Gujarat",          SamplePin = "380001" },
            new() { CityName = "Jaipur",          StateName = "Rajasthan",        SamplePin = "302001" },
            new() { CityName = "Surat",           StateName = "Gujarat",          SamplePin = "395001" },
            new() { CityName = "Lucknow",         StateName = "Uttar Pradesh",    SamplePin = "226001" },
            new() { CityName = "Kanpur",          StateName = "Uttar Pradesh",    SamplePin = "208001" },
            new() { CityName = "Nagpur",          StateName = "Maharashtra",      SamplePin = "440001" },
            new() { CityName = "Indore",          StateName = "Madhya Pradesh",   SamplePin = "452001" },
            new() { CityName = "Thane",           StateName = "Maharashtra",      SamplePin = "400601" },
            new() { CityName = "Bhopal",          StateName = "Madhya Pradesh",   SamplePin = "462001" },
            new() { CityName = "Visakhapatnam",   StateName = "Andhra Pradesh",   SamplePin = "530001" },
            new() { CityName = "Vadodara",        StateName = "Gujarat",          SamplePin = "390001" },
            new() { CityName = "Patna",           StateName = "Bihar",            SamplePin = "800001" },
            new() { CityName = "Ludhiana",        StateName = "Punjab",           SamplePin = "141001" },
            new() { CityName = "Coimbatore",      StateName = "Tamil Nadu",       SamplePin = "641001" },
            new() { CityName = "Agra",            StateName = "Uttar Pradesh",    SamplePin = "282001" },
            new() { CityName = "Madurai",         StateName = "Tamil Nadu",       SamplePin = "625001" },
            new() { CityName = "Nashik",          StateName = "Maharashtra",      SamplePin = "422001" },
            new() { CityName = "Varanasi",        StateName = "Uttar Pradesh",    SamplePin = "221001" },
            new() { CityName = "Meerut",          StateName = "Uttar Pradesh",    SamplePin = "250001" },
            new() { CityName = "Rajkot",          StateName = "Gujarat",          SamplePin = "360001" },
            new() { CityName = "Kalyan",          StateName = "Maharashtra",      SamplePin = "421301" },
            new() { CityName = "Aurangabad",      StateName = "Maharashtra",      SamplePin = "431001" },
            new() { CityName = "Dhanbad",         StateName = "Jharkhand",        SamplePin = "826001" },
            new() { CityName = "Amritsar",        StateName = "Punjab",           SamplePin = "143001" },
            new() { CityName = "Navi Mumbai",     StateName = "Maharashtra",      SamplePin = "400703" },
            new() { CityName = "Allahabad",       StateName = "Uttar Pradesh",    SamplePin = "211001" },
            new() { CityName = "Ranchi",          StateName = "Jharkhand",        SamplePin = "834001" },
            new() { CityName = "Ghaziabad",       StateName = "Uttar Pradesh",    SamplePin = "201001" },
            new() { CityName = "Howrah",          StateName = "West Bengal",      SamplePin = "711101" },
            new() { CityName = "Jabalpur",        StateName = "Madhya Pradesh",   SamplePin = "482001" },
            new() { CityName = "Gwalior",         StateName = "Madhya Pradesh",   SamplePin = "474001" },
            new() { CityName = "Vijayawada",      StateName = "Andhra Pradesh",   SamplePin = "520001" },
            new() { CityName = "Jodhpur",         StateName = "Rajasthan",        SamplePin = "342001" },
            new() { CityName = "Raipur",          StateName = "Chhattisgarh",     SamplePin = "492001" },
            new() { CityName = "Kota",            StateName = "Rajasthan",        SamplePin = "324001" },
            new() { CityName = "Chandigarh",      StateName = "Chandigarh",       SamplePin = "160001" },
            new() { CityName = "Guwahati",        StateName = "Assam",            SamplePin = "781001" },
            new() { CityName = "Solapur",         StateName = "Maharashtra",      SamplePin = "413001" },
            new() { CityName = "Hubli",           StateName = "Karnataka",        SamplePin = "580020" },
            new() { CityName = "Bareilly",        StateName = "Uttar Pradesh",    SamplePin = "243001" },
            new() { CityName = "Moradabad",       StateName = "Uttar Pradesh",    SamplePin = "244001" },
            new() { CityName = "Mysuru",          StateName = "Karnataka",        SamplePin = "570001" },
            new() { CityName = "Tiruppur",        StateName = "Tamil Nadu",       SamplePin = "641601" },
        };

        public static IReadOnlyList<IndianCityDto> GetAll() => _cities;

        public static IReadOnlyList<IndianCityDto> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return new List<IndianCityDto>();

            return _cities
                .Where(c => c.CityName.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();
        }
    }
}