using System.Collections.Generic;
using System.Threading.Tasks;
using Quasar.Hub.Models;

namespace Quasar.Hub.Services
{
	public interface IVideoService
	{
		string GetTwilioJwt(string identity);

		Task<IEnumerable<RoomDetails>> GetAllRoomsAsync();
	}
}