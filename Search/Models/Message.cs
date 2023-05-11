namespace Search.Models
{
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;

    public record Message
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; }

        public string Type { get; set; }

        /// <summary>
        /// Partition key
        /// </summary>
        public string SessionId { get; set; }

        public DateTime TimeStamp { get; set; }

        public string Sender { get; set; }

        public int? Tokens { get; set; }

        public string Text { get; set; }

        public Message(string sessionId, string sender, int? tokens, string text)
        {
            Id = Guid.NewGuid().ToString();
            Type = nameof(Message);
            SessionId = sessionId;
            Sender = sender;
            Tokens = tokens ?? 0;
            TimeStamp = DateTime.UtcNow;
            Text = text;
        }
    }
}