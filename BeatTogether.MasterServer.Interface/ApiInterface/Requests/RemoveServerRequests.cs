﻿namespace BeatTogether.MasterServer.Interface.ApiInterface.Requests
{
    public record RemoveServerRequest(string SecretOrCode, bool IsCode);

}
