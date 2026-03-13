using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloneEbay.Application.Notifications
{
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string html, CancellationToken ct);
    }
}
