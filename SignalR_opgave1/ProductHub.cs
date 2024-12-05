using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace SignalR_opgave1
{
    public class GameHub : Hub
    {
        private static ConcurrentDictionary<string, GameSession> ActiveGames = new();

        public async Task CreateGame(string gameId)
        {
            if (ActiveGames.Count >= 10)
            {
                await Clients.Caller.SendAsync("GameLimitReached");
                return;
            }

            if (!ActiveGames.ContainsKey(gameId))
            {
                ActiveGames[gameId] = new GameSession { GameId = gameId };
                await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
                await Clients.Caller.SendAsync("GameCreated", gameId);
            }
            else
            {
                await Clients.Caller.SendAsync("GameExists");
            }
        }

        public async Task JoinGame(string gameId)
        {
            if (ActiveGames.TryGetValue(gameId, out var game))
            {
                if (game.Player2 == null)
                {
                    game.Player2 = Context.ConnectionId;
                    await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
                    await Clients.Group(gameId).SendAsync("GameStarted", gameId);
                }
                else
                {
                    await Clients.Caller.SendAsync("GameFull");
                }
            }
            else
            {
                await Clients.Caller.SendAsync("GameNotFound");
            }
        }

        public async Task MakeMove(string gameId, int row, int col)
        {
            if (ActiveGames.TryGetValue(gameId, out var game))
            {
                string player = Context.ConnectionId;
                if(game.MakeMove(player, row, col))
                {
                    await Clients.Group(gameId).SendAsync("UpdateBoard", game.Board);
                    if(game.CheckWin())
                    {
                        await Clients.Group(gameId).SendAsync("GameWon", player);
                        ActiveGames.TryRemove(gameId, out _);
                    }
                    else if(game.CheckDraw())
                    {
                        await Clients.Group(gameId).SendAsync("GameDraw");
                        ActiveGames.TryRemove(gameId, out _);
                    }
                }
            }
        }
    }

    public class GameSession
    {
        public string GameId { get; set; }
        public string Player1 { get; set; }
        public string Player2 { get; set; }
        public char[,] Board { get; set; } = new Char[3, 3];
        private bool IsPlayer1Turn { get; set; } = true;

        public bool MakeMove(string player, int row, int col)
        {
            if (Board[row, col] == '\0' && ((IsPlayer1Turn && player == Player1) || (!IsPlayer1Turn && player == Player2)))
            {
                Board[row, col] = IsPlayer1Turn ? 'X' : 'O';
                IsPlayer1Turn = !IsPlayer1Turn;
                return true;
            }
            return false;
        }

        public bool CheckWin()
        {
            for (int i = 0; i < 3; i++)
            {
                // Rows and Columns
                if (Board[i, 0] != '\0' && Board[i, 0] == Board[i, 1] && Board[i, 1] == Board[i, 2]) return true;
                if (Board[0, i] != '\0' && Board[0, i] == Board[1, i] && Board[1, i] == Board[2, i]) return true;
            }

            // Diagonals
            if (Board[0, 0] != '\0' && Board[0, 0] == Board[1, 1] && Board[1, 1] == Board[2, 2]) return true;
            if (Board[0, 2] != '\0' && Board[0, 2] == Board[1, 1] && Board[1, 1] == Board[2, 0]) return true;

            return false;
        }

        public bool CheckDraw() => Board.Cast<char>().All(cell => cell != '\0');
    }
}
