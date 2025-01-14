﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BeatTogether.MasterServer.Data.Abstractions.Repositories;
using BeatTogether.MasterServer.Domain.Enums;
using BeatTogether.MasterServer.Domain.Models;

namespace BeatTogether.MasterServer.Data.Implementations.Repositories
{
    public sealed class MemoryServerRepository : IServerRepository
    {
        private static ConcurrentDictionary<string, Server> _servers = new();
        private static ConcurrentDictionary<string, string> _secretsByCode = new();
        private long TotalJoins = 0;

        public Task<long> TotalPlayerJoins()
        {
            return Task.FromResult(TotalJoins);
        }

        public Task<string[]> GetPublicServerSecretsList()
        {
            return Task.FromResult(_servers.Keys.ToArray());
        }
        public Task<Server[]> GetPublicServerList()
        {
            return Task.FromResult(_servers.Values.ToArray());
        }

        public Task<string[]> GetServerSecretsList()
        {
            List<string> secrets = new();
            foreach (var server in _servers.Values)
            {
                if (server.IsPublic)
                    secrets.Add(server.Secret);
            }
            return Task.FromResult(secrets.ToArray());
        }

        public Task<Server[]> GetServerList()
        {
            return Task.FromResult((_servers.Values.Where(value => value.IsPublic)).ToArray());
        }

        public Task<int> GetPublicServerCount()
        {
            return Task.FromResult((_servers.Values.Where(value => value.IsPublic)).Count());
        }
        public Task<int> GetServerCount()
        {
            return Task.FromResult(_servers.Count);
        }

        public Task<Server> GetServer(string secret)
        {
            if (!_servers.TryGetValue(secret, out var server))
                return Task.FromResult<Server>(null);
            return Task.FromResult(server);
        }

        public Task<Server> GetServerByCode(string code)
        {
            if (!_secretsByCode.TryGetValue(code, out var secret))
                return Task.FromResult<Server>(null);
            if (!_servers.TryGetValue(secret, out var server))
                return Task.FromResult<Server>(null);
            return Task.FromResult(server);
        }

        public Task<Server> GetAvailablePublicServer(
            InvitePolicy invitePolicy, 
            GameplayServerMode serverMode, 
            SongSelectionMode songMode, 
            GameplayServerControlSettings serverControlSettings, 
            BeatmapDifficultyMask difficultyMask, 
            GameplayModifiersMask modifiersMask, 
            ulong songPackTop, 
            ulong songPackBottom)
        {
            if (!_servers.Any())
                return Task.FromResult<Server>(null);
            var publicServers = _servers.Values.Where(server => 
                server.GameplayServerConfiguration.DiscoveryPolicy == DiscoveryPolicy.Public &&
                server.GameplayServerConfiguration.InvitePolicy == invitePolicy &&
                server.GameplayServerConfiguration.GameplayServerMode == serverMode &&
                server.GameplayServerConfiguration.SongSelectionMode == songMode &&
                server.GameplayServerConfiguration.GameplayServerControlSettings == serverControlSettings &&
                server.BeatmapDifficultyMask == difficultyMask &&
                server.GameplayModifiersMask == modifiersMask &&
                server.SongPackBloomFilterTop == songPackTop &&
                server.SongPackBloomFilterBottom == songPackBottom &&
                server.IsInGameplay == false
            );
            if (!publicServers.Any())
                return Task.FromResult<Server>(null);
            var server = publicServers.First();
            foreach (var publicServer in publicServers)
            {
                if (publicServer.CurrentPlayerCount < server.CurrentPlayerCount)
                    server = publicServer;
                if (server.CurrentPlayerCount <= 1)
                    break;
            }
            if (server.CurrentPlayerCount >= server.GameplayServerConfiguration.MaxPlayerCount)
                return Task.FromResult<Server>(null);
            return Task.FromResult(server);
        }

        public Task<bool> AddServer(Server server)
        {
            if (!_servers.TryAdd(server.Secret, server))
                return Task.FromResult(false);
            _secretsByCode[server.Code] = server.Secret;
            return Task.FromResult(true);
        }

        public Task<bool> RemoveServer(string secret)
        {
            if (!_servers.TryRemove(secret, out var server))
                return Task.FromResult(false);
            _secretsByCode.TryRemove(server.Code, out _);
            return Task.FromResult(true);
        }
        public Task<bool> RemoveServersWithEndpoint(IPAddress EndPoint)
        {
            List<string> secrets = new();
            foreach (var server in _servers)
            {
                if(server.Value.RemoteEndPoint.Address.ToString() == EndPoint.ToString())
                {
                    secrets.Add(server.Key);
                }
            }
            foreach (string secret in secrets)
            {
                RemoveServer(secret);
            }
            return Task.FromResult(true);
        }

        public Task<bool> IncrementCurrentPlayerCount(string secret)
        {
            if (!_servers.TryGetValue(secret, out var server))
                return Task.FromResult(false);
            server.CurrentPlayerCount++;
            Interlocked.Increment(ref TotalJoins);
            return Task.FromResult(true);
        }

        public Task<bool> DecrementCurrentPlayerCount(string secret)
        {
            if (!_servers.TryGetValue(secret, out var server))
                return Task.FromResult(false);
            server.CurrentPlayerCount--;
            return Task.FromResult(true);
        }

        public Task<bool> UpdateCurrentPlayerCount(string secret, int currentPlayerCount)
        {
            if (!_servers.TryGetValue(secret, out var server))
                return Task.FromResult(false);
            server.CurrentPlayerCount = currentPlayerCount;
            return Task.FromResult(true);
        }

        public Task<bool> UpdateServerGameplayState(string secret, bool InGameplay)
        {
            if (!_servers.TryGetValue(secret, out var server))
                return Task.FromResult(false);
            server.IsInGameplay = InGameplay;
            return Task.FromResult(true);
        }
    }
}