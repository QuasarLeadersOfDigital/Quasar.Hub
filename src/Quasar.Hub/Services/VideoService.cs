using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quasar.Hub.Models;
using Quasar.Hub.Settings;
using Twilio;
using Twilio.Base;
using Twilio.Jwt.AccessToken;
using Twilio.Rest.Video.V1;
using Twilio.Rest.Video.V1.Room;
using ParticipantStatus = Twilio.Rest.Video.V1.Room.ParticipantResource.StatusEnum;

namespace Quasar.Hub.Services
{
	public class VideoService : IVideoService
	{
		private readonly TwilioSettings _twilioSettings;

		public VideoService(Microsoft.Extensions.Options.IOptions<TwilioSettings> twilioOptions)
		{
			_twilioSettings =
				twilioOptions?.Value
				?? throw new ArgumentNullException(nameof(twilioOptions));

			TwilioClient.Init(_twilioSettings.ApiKey, _twilioSettings.ApiSecret);
		}

		public string GetTwilioJwt(string identity)
			=> new Token(_twilioSettings.AccountSid,
				_twilioSettings.ApiKey,
				_twilioSettings.ApiSecret,
				identity ?? Guid.NewGuid().ToString(),
				grants: new HashSet<IGrant> { new VideoGrant() }).ToJwt();

		public async Task<IEnumerable<RoomDetails>> GetAllRoomsAsync()
		{
			ResourceSet<RoomResource> rooms = await RoomResource.ReadAsync();
			IEnumerable<Task<RoomDetails>> tasks = rooms.Select(
				room => GetRoomDetailsAsync(
					room,
					ParticipantResource.ReadAsync(
						room.Sid,
						ParticipantStatus.Connected)));

			return await Task.WhenAll(tasks);
		}
		

		private async Task<RoomDetails> GetRoomDetailsAsync(
			RoomResource room,
			Task<ResourceSet<ParticipantResource>> participantTask)
		{
			ResourceSet<ParticipantResource> participants = await participantTask;
			return new RoomDetails
			{
				Name = room.UniqueName,
				MaxParticipants = room.MaxParticipants ?? 0,
				ParticipantCount = participants.ToList().Count
			};
		}
	}
}
