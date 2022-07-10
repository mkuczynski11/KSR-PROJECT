using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using MassTransit;
using Common;
using System;
using Warehouse.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Warehouse.Configuration;
using Microsoft.Extensions.Configuration;

namespace Warehouse.Controllers
{
    [ApiController]
    [Route("warehouse/[controller]")]
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
        [HttpGet("reservations")]
        public IEnumerable<Reservation> GetReservations()
        {
            var collection = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                .GetCollection<Reservation>(_mongoConf.CollectionName.Reservations);
            return collection.Find(_ => true).ToList();
        }
        [HttpPost("create")]
        public ActionResult<BookResponse> CreateBook([FromBody] BookRequest request)
        {
            _logger.LogInformation($"New Book: name={request.Name}, quantity={request.Quantity}, price={request.Price}, discount={request.Discount} requested to be created");
            string ID = Guid.NewGuid().ToString();

            Book book = new Book(ID, request.Name, request.Quantity);

            var collection = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                .GetCollection<Book>(_mongoConf.CollectionName.Books);

            collection.InsertOne(book);

            _logger.LogInformation($"Sending info: BookID={ID}, price={request.Price} to sales department");
            _publishEndpoint.Publish<NewBookSalesInfo>(new
            {
                ID = ID,
                price = request.Price
            });

            _logger.LogInformation($"Sending info: BookID={ID}, price={request.Discount} to marketing department");
            _publishEndpoint.Publish<NewBookMarketingInfo>(new
            {
                ID = ID,
                discount = request.Discount
            });

            return new BookResponse { ID = book.ID };
        }
        [HttpPut]
        public ActionResult<Book> PutBook([FromBody] BookUpdateRequest request)
        {
            var collection = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                .GetCollection<Book>(_mongoConf.CollectionName.Books);

            Book book = collection.Find(b => b.ID.Equals(request.ID)).SingleOrDefault();

            if (book == null) return NotFound();
            
            book.Name = request.Name;
            book.Quantity = request.Quantity;

            collection.ReplaceOne(b => b.ID.Equals(request.ID), book);

            return book;
        }
    }
}
