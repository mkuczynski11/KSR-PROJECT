using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using MassTransit;
using Marketing.Models;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Marketing.Controllers
{
    [ApiController]
    [Route("marketing/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly ILogger<BooksController> _logger;
        private BookContext _bookContext;
        public readonly IPublishEndpoint _publishEndpoint;

        public BooksController(IPublishEndpoint publishEndpoint, BookContext bookContext, ILogger<BooksController> logger)
        {
            _logger = logger;
            _bookContext = bookContext;
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        public IEnumerable<Book> GetBooks()
        {
            return _bookContext.BookItems;
        }
        [HttpPut]
        public ActionResult<Book> PutBook([FromBody] BookUpdateRequest request)
        {
            Book book = _bookContext.BookItems.SingleOrDefault(b => b.ID.Equals(request.BookID));

            if (book == null) return NotFound();

            book.Discount = request.BookDiscount;
            _bookContext.SaveChanges();

            return book;
        }
    }
}
