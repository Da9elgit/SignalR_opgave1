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
        private static string currentPlayer = "Player 1";
        

        public override async Task OnConnectedAsync()
        {
            if (Id1 == "")
            {
                Id1 = Context.ConnectionId;            
                await Clients.Caller.SendAsync("PlayerIdStatus", currentPlayer, Id1);
            }
            else if (Id2 == "")
            {
                Id2 = Context.ConnectionId;
                currentPlayer = "Player 2";
                await Clients.Caller.SendAsync("PlayerIdStatus", currentPlayer, Id2);
                currentPlayer = "Player 1";
            }
            // Send the current grid state to the new client
            await Clients.Caller.SendAsync("UpdateGrid", Grid);
            await base.OnConnectedAsync();
        }

        public async Task ClickCell(int i)
        {
            Console.WriteLine($"ClickCell called by {Context.ConnectionId} for index {i}.");
            

            if (Id1 == Context.ConnectionId && currentPlayer == "Player 1")
            {
                Grid[i] = "red";
                string playerColor = "red";
                if (CheckForWin(playerColor))
                {
                    await Clients.All.SendAsync("DeclareWinner", currentPlayer);
                    Console.WriteLine($"{currentPlayer} wins!");
                    return;
                }
                currentPlayer = "Player 2";
            }
            else if (Id2 == Context.ConnectionId && currentPlayer == "Player 2")
            {
                Grid[i] = "blue";
                string playerColor = "blue";
                if (CheckForWin(playerColor))
                {
                    await Clients.All.SendAsync("DeclareWinner", currentPlayer);
                    Console.WriteLine($"{currentPlayer} wins!");
                    return;
                }
                currentPlayer = "Player 1";
            }
            await Clients.All.SendAsync("PlayerTurnStatus", currentPlayer);

            

            // Update grid with player's color
            await Clients.All.SendAsync("UpdateGrid", Grid);

        }

        public async Task ResetGrid()
        {
            Console.WriteLine("ResetGrid called.");

            // Clear the grid

            for (int i = 0; i < GridSize * GridSize; i++)
            {
                Grid[i] = null;
            }

            // Notify clients about the reset
            await Clients.All.SendAsync("UpdateGrid", Grid);
        }

        private bool CheckForWin(string playerColor)
        {
            // Check rows, columns, and diagonals
            return CheckRows(playerColor) || CheckColumns(playerColor) || CheckDiagonals(playerColor);
        }

        private bool CheckRows(string playerColor)
        {
            for (int row = 0; row < GridSize; row++)
            {
                bool win = true;
                for (int col = 0; col < GridSize; col++)
                {
                    if (Grid[row * GridSize + col] != playerColor)
                    {
                        win = false;
                        break;
                    }
                }
                if (win) return true;
            }
            return false;
        }

        private bool CheckColumns(string playerColor)
        {
            for (int col = 0; col < GridSize; col++)
            {
                bool win = true;
                for (int row = 0; row < GridSize; row++)
                {
                    if (Grid[row * GridSize + col] != playerColor)
                    {
                        win = false;
                        break;
                    }
                }
                if (win) return true;
            }
            return false;
        }
        private bool CheckDiagonals(string playerColor)
        {
            // Check main diagonal
            bool mainDiagonalWin = true;
            for (int i = 0; i < GridSize; i++)
            {
                if (Grid[i * GridSize + i] != playerColor)
                {
                    mainDiagonalWin = false;
                    break;
                }
            }

            // Check anti-diagonal
            bool antiDiagonalWin = true;
            for (int i = 0; i < GridSize; i++)
            {
                if (Grid[i * GridSize + (GridSize - i - 1)] != playerColor)
                {
                    antiDiagonalWin = false;
                    break;
                }
            }

            return mainDiagonalWin || antiDiagonalWin;
        }
    }
}
