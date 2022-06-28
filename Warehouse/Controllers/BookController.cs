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
        public IEnumerable<Book> GetBooks()
        {
            return _bookContext.BookItems;
        }
        [HttpGet("reservations")]
        public IEnumerable<Reservation> GetReservations()
        {
            return _bookContext.ReservationItems;
        }
        [HttpPost("create")]
        public ActionResult<BookResponse> CreateBook([FromBody] BookRequest request)
        {
            Console.WriteLine($"New Book:{request.Name}, quantity={request.Quantity}, price={request.Price}, discount={request.Discount} requested to be created");
            string ID = Guid.NewGuid().ToString();

            Book book = new Book(ID, request.Name, request.Quantity);

            _bookContext.BookItems.Add(book);
            _bookContext.SaveChanges();

            Console.WriteLine($"Sending info:{ID}, price={request.Price} to sales department");
            _publishEndpoint.Publish<NewBookSalesInfo>(new
            {
                ID = ID,
                price = request.Price
            });

            Console.WriteLine($"Sending info:{ID}, price={request.Discount} to marketing department");
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
            Book book = _bookContext.BookItems.SingleOrDefault(b => b.ID.Equals(request.ID));

            if (book == null) return NotFound();
            
            book.Name = request.Name;
            book.Quantity = request.Quantity;

            _bookContext.SaveChanges();

            return book;
        }
    }
}
