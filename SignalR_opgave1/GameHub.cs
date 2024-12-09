using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Linq;

namespace SignalR_opgave1
{
    public class GameHub : Hub
    {
        private static ConcurrentDictionary<string, GameSession> ActiveGames = new();

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Clean up disconnected players
            var game = ActiveGames.Values.FirstOrDefault(g => g.Player1 == Context.ConnectionId || g.Player2 == Context.ConnectionId);
            if (game != null)
            {
                ActiveGames.TryRemove(game.GameId, out _);
                await Clients.All.SendAsync("UpdateAvailableGames", GetAvailableGames());
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task CreateGame(string gameId)
        {
            Console.WriteLine($"CreateGame called by {Context.ConnectionId} with gameId: {gameId}");

            if (ActiveGames.Count >= 10)
            {
                await Clients.Caller.SendAsync("GameLimitReached");
                return;
            }

            if (!ActiveGames.ContainsKey(gameId))
            {
                ActiveGames[gameId] = new GameSession { GameId = gameId, Player1 = Context.ConnectionId };
                await Groups.AddToGroupAsync(Context.ConnectionId, gameId);

                Console.WriteLine($"Game {gameId} created by {Context.ConnectionId}. Waiting for another player.");
                await Clients.Caller.SendAsync("GameCreated", gameId);
                await Clients.All.SendAsync("UpdateAvailableGames", GetAvailableGames());
            }
            else
            {
                Console.WriteLine($"Game ID {gameId} already exists.");
                await Clients.Caller.SendAsync("GameExists");
            }
        }

        public async Task JoinGame(string gameId)
        {
            Console.WriteLine($"JoinGame called by {Context.ConnectionId} for gameId: {gameId}");

            if (ActiveGames.TryGetValue(gameId, out var game))
            {
                if (game.Player2 == null)
                {
                    game.Player2 = Context.ConnectionId;
                    await Groups.AddToGroupAsync(Context.ConnectionId, gameId);

                    // Notify both players the game has started
                    Console.WriteLine($"Player {Context.ConnectionId} joined game {gameId}. Game is starting.");
                    await Clients.Group(gameId).SendAsync("GameStarted", gameId);
                    await Clients.All.SendAsync("UpdateAvailableGames", GetAvailableGames());
                    // Send the initial board state
                    await Clients.Group(gameId).SendAsync("UpdateBoard", game.Board);

                }
                else
                {
                    Console.WriteLine($"Game {gameId} is full. Player {Context.ConnectionId} cannot join.");
                    await Clients.Caller.SendAsync("GameFull");
                }
            }
            else
            {
                Console.WriteLine($"Game {gameId} not found. Player {Context.ConnectionId} cannot join.");
                await Clients.Caller.SendAsync("GameNotFound");
            }
        }

        public async Task MakeMove(string gameId, int row, int col)
        {
            Console.WriteLine($"MakeMove called by {Context.ConnectionId} for game {gameId} at cell ({row}, {col})");

            // Check if the game exists
            if (ActiveGames.TryGetValue(gameId, out var game))
            {
                string player = Context.ConnectionId == game.Player1 ? "Player1" : "Player2";
                Console.WriteLine($"{player} ({Context.ConnectionId}) is making a move.");

                // Attempt to make the move
                bool moveSuccessful = game.MakeMove(player, row, col);
                if (moveSuccessful)
                {
                    Console.WriteLine($"Move successful for {player} at cell ({row}, {col}).");

                    // Update the board for all players in the game
                    await Clients.Group(gameId).SendAsync("UpdateBoard", game.Board);

                    // Check for a win condition
                    if (game.CheckWin())
                    {
                        Console.WriteLine($"Game {gameId}: {player} won the game.");
                        await Clients.Group(gameId).SendAsync("GameWon", player);

                        // Remove the game and update available games
                        ActiveGames.TryRemove(gameId, out _);
                        await Clients.All.SendAsync("UpdateAvailableGames", GetAvailableGames());
                    }
                    // Check for a draw condition
                    else if (game.CheckDraw())
                    {
                        Console.WriteLine($"Game {gameId}: The game ended in a draw.");
                        await Clients.Group(gameId).SendAsync("GameDraw");

                        // Remove the game and update available games
                        ActiveGames.TryRemove(gameId, out _);
                        await Clients.All.SendAsync("UpdateAvailableGames", GetAvailableGames());
                    }
                }
                else
                {
                    Console.WriteLine($"Invalid move attempted by {player} at cell ({row}, {col}).");
                }
            }
            else
            {
                Console.WriteLine($"Game {gameId} not found. MakeMove failed for {Context.ConnectionId}.");
            }
        }

        public async Task RequestAvailableGames()
        {
            await Clients.Caller.SendAsync("UpdateAvailableGames", GetAvailableGames());
        }
        private static List<string> GetAvailableGames()
        {
            return ActiveGames.Values
                .Where(game => game.Player2 == null)
                .Select(game => game.GameId)
                .ToList();
        }

        public async Task RequestBoardState(string gameId)
        {
            if (ActiveGames.TryGetValue(gameId, out var game))
            {
                await Clients.Caller.SendAsync("UpdateBoard", game.Board);
            }
            else
            {
                await Clients.Caller.SendAsync("GameNotFound");
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
