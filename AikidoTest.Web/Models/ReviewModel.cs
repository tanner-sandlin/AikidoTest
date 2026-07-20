using System;

namespace AikidoTest.Web.Models
{
    public class ReviewModel
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public string Author { get; set; }
        public string Body { get; set; }
        public string AvatarUrl { get; set; }
        public string AttachmentPath { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
