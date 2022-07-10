using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using MassTransit;
using Sales.Models;
using System.Linq;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Sales.Configuration;
using Microsoft.Extensions.Configuration;

namespace Sales.Controllers
{
    [ApiController]
    [Route("sales/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly ILogger<BooksController> _logger;
        private readonly MongoClient _mongoClient;
        private readonly MongoDbConfiguration _mongoConf;
        public readonly IPublishEndpoint _publishEndpoint;

        public BooksController(IPublishEndpoint publishEndpoint, MongoClient mongoClient, ILogger<BooksController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _mongoClient = mongoClient;
            _mongoConf = configuration.GetSection("MongoDb").Get<MongoDbConfiguration>();
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        public IEnumerable<Book> GetBooks()
        {
            var collection = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                .GetCollection<Book>(_mongoConf.CollectionName.Books);
            return collection.Find(_ => true).ToList();
        }
        [HttpPut]
        public ActionResult<Book> PutBook([FromBody] BookUpdateRequest request)
        {
            var collection = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                .GetCollection<Book>(_mongoConf.CollectionName.Books);

            Book book = collection.Find(o => o.ID.Equals(request.BookID)).SingleOrDefault();

            if (book == null) return NotFound();

            book.Price = request.BookPrice;
            collection.ReplaceOne(o => o.ID.Equals(request.BookID), book);

            return book;
        }
    }
}
