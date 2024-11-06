// Models/Item.cs
using System.ComponentModel.DataAnnotations;

namespace DNS.Models
{
    public class Dns
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string primeryDns { get; set; }
        public string secondaryDns { get; set; }
    }
}
