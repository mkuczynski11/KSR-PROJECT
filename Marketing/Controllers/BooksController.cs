using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using MassTransit;
using Marketing.Models;

namespace Marketing.Controllers
{
    [ApiController]
    [Route("marketing/[controller]")]
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
    }
}
