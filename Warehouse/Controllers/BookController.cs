using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using MassTransit;
using System.Threading.Tasks;
using Common;
using System;
using Warehouse.Models;

namespace Warehouse.Controllers
{
    [ApiController]
    [Route("warehouse/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly BookContext _bookContext;
        public readonly IPublishEndpoint _publishEndpoint;

        public BooksController(IPublishEndpoint publishEndpoint, BookContext bookContext)
        {
            _bookContext = bookContext;
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        public IEnumerable<Book> Get()
        {
            return _bookContext.BookItems;
        }
        // TODO: decide if we should keep an option to get a single book from warehouse
        [HttpGet("{id}")]
        public BookResponse GetBook(string id)
        {
            Console.WriteLine($"Book with ID:{id} requested");
            Book book = _bookContext.BookItems.SingleOrDefault(b => b.ID.Equals(id));
            if (book == null) return new BookResponse { name = "", quantity = -1, price = -1, discount = -1 };

            // TODO: saga in which we ask sales and marketing for the book info

            return new BookResponse { name = book.name, quantity = book.quantity, price = -1, discount = -1 };
        }
        [HttpPost("create")]
        public async Task<IActionResult> CreateBook([FromBody] BookRequest request)
        {
            Console.WriteLine($"New Book:{request.name}, quantity={request.quantity}, price={request.price}, discount={request.discount} requested to be created");
            string ID = Guid.NewGuid().ToString();

            Book book = new Book(ID, request.name, request.quantity);

            _bookContext.BookItems.Add(book);
            _bookContext.SaveChanges();

            Console.WriteLine($"Sending info:{ID}, price={request.price} to sales department");
            await _publishEndpoint.Publish<NewBookSalesInfo>(new
            {
                ID = ID,
                price = request.price
            });

            Console.WriteLine($"Sending info:{ID}, price={request.discount} to marketing department");
            await _publishEndpoint.Publish<NewBookMarketingInfo>(new
            {
                ID = ID,
                discount = request.discount
            });

            return Ok();
        }
        //TODO: remove since it is here for testing purposes
        [HttpPost("testSaga/test")]
        public async Task<IActionResult> TestSaga()
        {
            Console.WriteLine("testing saga");
            Book book = new Book("asd", "name", 1);
            _bookContext.BookItems.Add(book);
            _bookContext.SaveChanges();
            await _publishEndpoint.Publish<ShippingRequest>(new
            {
                ID = "asd",
                quantity = 1
            });

            return Ok();
        }
        //TODO: remove since it is here for testing purposes
        [HttpPost("test/checkTest")]
        public async Task<IActionResult> CheckTest()
        {
            Console.WriteLine("testing check messages");
            Book book = new Book("asd", "name", 13);
            _bookContext.BookItems.Add(book);
            _bookContext.SaveChanges();
            await _publishEndpoint.Publish<BookQuantityCheck>(new
            {
                ID = "asd",
                quantity = 8
            });

            await _publishEndpoint.Publish<DeliveryCheck>(new
            {
                price = 10.0,
                method = "DPD"
            });

            return Ok();
        }
    }
}
