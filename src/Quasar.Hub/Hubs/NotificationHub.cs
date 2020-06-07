using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Quasar.Hub.Hubs
{
	public class NotificationHub : Microsoft.AspNetCore.SignalR.Hub
	{
		public async Task RoomsUpdated(bool flag)
			=> await Clients.Others.SendAsync("RoomsUpdated", flag);
	}
}
