using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using MassTransit;
using System.Threading.Tasks;
using Common;
using System;

namespace Sales.Controllers
{
    [ApiController]
    [Route("sales/[controller]")]
    public class BooksController : ControllerBase
    {
        public readonly IPublishEndpoint _publishEndpoint;
        private static readonly List<Book> Books = new List<Book>();

        public BooksController(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        public IEnumerable<Book> GetBooks()
        {
            return Books;
        }
    }
}
