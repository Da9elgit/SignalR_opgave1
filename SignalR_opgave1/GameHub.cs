using Microsoft.AspNetCore.SignalR;
using System.Drawing;

namespace SignalR_opgave1
{
    public class GameHub : Hub
    {
        private static readonly int GridSize = 5;
        private static readonly string[] Grid = new string[GridSize * GridSize];
        private static string Id1 = "";
        private static string Id2 = ""; 

        public override async Task OnConnectedAsync()
        {
            if(Id1 == "")
            {
                Id1 = Context.ConnectionId;
            }
            else if(Id2 == "") 
            {
                Id2 = Context.ConnectionId;
            }
            // Send the current grid state to the new client
            await Clients.Caller.SendAsync("UpdateGrid", Grid);
            await base.OnConnectedAsync();
        }

        public async Task ClickCell(int i)
        {
            if(Id1 == Context.ConnectionId)
            {
                Console.WriteLine("");
            }
            else if(Id2 == Context.ConnectionId)
            {
                Console.WriteLine("");
            }

            if (i<0 || i>= GridSize*GridSize)
                return;

            // Update grid with player's color
            Grid[i] = "Red";
                await Clients.All.SendAsync("UpdateGrid", Grid);
            
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
