using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using MassTransit;
using System.Threading.Tasks;
using Common;
using System;

namespace Warehouse.Controllers
{
    [ApiController]
    [Route("warehouse/[controller]")]
    public class BooksController : ControllerBase
    {
        public readonly IPublishEndpoint _publishEndpoint;
        private static readonly List<Book> Books = new List<Book>();

        public BooksController(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        public IEnumerable<Book> Get()
        {
            return Books;
        }
        // TODO: decide if we should keep an option to get a single book from warehouse
        [HttpGet("{id}")]
        public BookResponse GetBook(string id)
        {
            Console.WriteLine($"Book with ID:{id} requested");
            Book book = Books.SingleOrDefault(b => b.ID.Equals(id));
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

            Books.Add(book);

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
    }
}
