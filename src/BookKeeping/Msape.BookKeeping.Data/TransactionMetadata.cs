namespace Msape.BookKeeping.Data
{
    //I'll use this later to hold metadata that I don't really need for processing
    public class TransactionMetadata
    {
        public ulong TransactionId { get; set; }

        public UserData Data { get; set; }

        //add properties here, they will be serialized to json
        public class UserData
        {

        }
    }
}
