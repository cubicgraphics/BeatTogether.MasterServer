﻿using BeatTogether.MasterServer.Messaging.Enums;

namespace BeatTogether.MasterServer.Interface.ApiInterface.Models
{
    public record MServerPlayer(
     Platform Platform,
     string UserId,
     string UserName,
     string Secret);
}
