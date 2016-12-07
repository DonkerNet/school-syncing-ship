namespace SyncingShip.Protocol
{
    public class SyncVerbs
    {
        public const string List = "LIST";
        public const string Get = "GET";
        public const string Put = "PUT";
        public const string Delete = "DELETE";

        public static string[] RequestVerbs =
        {
            List,
            Get,
            Put,
            Delete
        };

        public const string Response = "RESPONSE";

        public static string[] ResponseVerbs =
        {
            Response
        };
    }
}