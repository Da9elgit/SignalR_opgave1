using Microsoft.AspNetCore.SignalR;

namespace SignalR_opgave1
{
    public class GameHub : Hub
    {
        private static readonly int GridSize = 5;
        private static readonly string[] Grid = new string[GridSize * GridSize];
        private static readonly Dictionary<string, string> PlayerColors = new()
        {
            { "Player1", "red" },
            { "Player2", "blue" }
        };

        public override async Task OnConnectedAsync()
        {
            // Assign players to Player1 or Player2
            if (!PlayerColors.ContainsKey(Context.ConnectionId))
            {
                var playerCount = PlayerColors.Count;
                var playerKey = playerCount == 0 ? "Player1" : "Player2";
                PlayerColors[Context.ConnectionId] = PlayerColors[playerKey];
            }

            // Send the current grid state to the new client
            await Clients.Caller.SendAsync("UpdateGrid", Grid);
            await base.OnConnectedAsync();
        }

        public async Task ClickCell(int i)
        {
            Console.WriteLine("ClickCell called.");

            if (i<0 || i>= GridSize*GridSize)
                return;

            // Update grid with player's color
            if (PlayerColors.TryGetValue(Context.ConnectionId, out var color))
            {
                Grid[i] = color;
                await Clients.All.SendAsync("UpdateGrid", Grid);
            }
        }

        public async Task ResetGrid()
        {
            Console.WriteLine("ResetGrid called.");

            // Clear the grid

            for (int i = 0; i < GridSize*GridSize; i++)
            {
                Grid[i] = null;
            }


            await Clients.All.SendAsync("UpdateGrid", Grid);
        }
    }
}
