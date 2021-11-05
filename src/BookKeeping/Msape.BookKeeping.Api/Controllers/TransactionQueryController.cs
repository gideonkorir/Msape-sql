using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Msape.BookKeeping.Api.Models;
using Msape.BookKeeping.Data.EF;
using System;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Api.Controllers
{
    [Route("api/transactions")]
    [ApiController]
    public partial class TransactionQueryController : ControllerBase
    {
        private readonly BookKeepingContext _bookKeepingContext;

        public TransactionQueryController(BookKeepingContext bookKeepingContext)
        {
            _bookKeepingContext = bookKeepingContext ?? throw new ArgumentNullException(nameof(bookKeepingContext));
        }

        [HttpGet("{id:ulong}", Name = "GetTxById")]
        public async Task<IActionResult> Get([FromRoute] ulong id)
        {
            var response = await _bookKeepingContext.Transactions
                    .FindAsync(new object[] { id }, HttpContext.RequestAborted)
                    .ConfigureAwait(false);

            return response != null
                ? Ok(TransactionApiModel.Create(response))
                : NotFound(new
                {
                    ErrorMessage = $"Transaction with id {id} was not found"
                });
        }

        [HttpGet("{receipt}", Name = "GetTxByReceipt")]
        public async Task<IActionResult> Get([FromRoute] string receipt)
        {
            var response = await _bookKeepingContext.Transactions
                    .SingleAsync(c => c.ReceiptNumber == receipt)
                    .ConfigureAwait(false);

            return response != null
                ? Ok(TransactionApiModel.Create(response))
                : NotFound(new
                {
                    ErrorMessage = $"Transaction with receipt {receipt} was not found"
                });
        }
    }
}
