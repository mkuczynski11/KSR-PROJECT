using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using MassTransit;
using Sales.Models;
using System.Linq;

namespace Sales.Controllers
{
    [ApiController]
    [Route("sales/[controller]")]
    public class BooksController : ControllerBase
    {
        private BookContext _bookContext;
        public readonly IPublishEndpoint _publishEndpoint;

        public BooksController(IPublishEndpoint publishEndpoint, BookContext bookContext)
        {
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

            book.Price = request.BookPrice;
            _bookContext.SaveChanges();

            return book;
        }
    }
}
