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
        public Book GetBook(string id)
        {
            return Books.SingleOrDefault(b => b.ID.Equals(id));
        }
        [HttpPost("create")]
        public async Task<IActionResult> CreateBook([FromBody] BookRequest request)
        {
            string ID = Guid.NewGuid().ToString();

            Book book = new Book(ID, request.name, request.quantity);

            Books.Add(book);

            await _publishEndpoint.Publish<NewBookSalesInfo>(new
            {
                ID = ID,
                price = request.price
            });

            await _publishEndpoint.Publish<NewBookMarketingInfo>(new
            {
                ID = ID,
                discount = request.discount
            });

            return Ok();
        }
    }
}
