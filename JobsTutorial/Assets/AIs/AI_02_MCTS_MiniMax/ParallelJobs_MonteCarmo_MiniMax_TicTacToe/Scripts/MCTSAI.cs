

/*
namespace ParallelJobs_MonteCarmo_MiniMax
{
    public enum GridState { Empty, X, O }

    public class MCTSAI : MonoBehaviour
    {
        public Text statusText;
        public Button[] gridButtons;

        private GridState[,] gameBoard = new GridState[3, 3];
        private bool isXTurn = true;
        private bool isGameEnded = false;

        private void Start()
        {
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    gameBoard[i, j] = GridState.Empty;
                }
            }
            UpdateBoardDisplay();
            statusText.text = "Player X's turn";
        }

        private void UpdateBoardDisplay()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    int index = i * 3 + j;
                    if (gameBoard[i, j] == GridState.Empty)
                    {
                        gridButtons[index].GetComponentInChildren<Text>().text = "";
                    }
                    else if (gameBoard[i, j] == GridState.X)
                    {
                        gridButtons[index].GetComponentInChildren<Text>().text = "X";
                    }
                    else if (gameBoard[i, j] == GridState.O)
                    {
                        gridButtons[index].GetComponentInChildren<Text>().text = "O";
                    }
                }
            }
        }

        private bool CheckForWinner()
        {
            // Check rows
            for (int i = 0; i < 3; i++)
            {
                if (gameBoard[i, 0] != GridState.Empty && gameBoard[i, 0] == gameBoard[i, 1] && gameBoard[i, 0] == gameBoard[i, 2])
                {
                    statusText.text = "Player " + (gameBoard[i, 0] == GridState.X ? "X" : "O") + " wins!";
                    return true;
                }
            }

            // Check columns
            for (int j = 0; j < 3; j++)
            {
                if (gameBoard[0, j] != GridState.Empty && gameBoard[0, j] == gameBoard[1, j] && gameBoard[0, j] == gameBoard[2, j])
                {
                    statusText.text = "Player " + (gameBoard[0, j] == GridState.X ? "X" : "O") + " wins!";
                    return true;
                }
            }

            // Check diagonals
            if (gameBoard[0, 0] != GridState.Empty && gameBoard[0, 0] == gameBoard[1, 1] && gameBoard[0, 0] == gameBoard[2, 2])
            {
                statusText.text = "Player " + (gameBoard[0, 0] == GridState.X ? "X" : "O") + " wins!";
                return true;
            }
            if (gameBoard[0, 2] != GridState.Empty && gameBoard[0, 2] == gameBoard[1, 1] && gameBoard[0, 2] == gameBoard[2, 0])
            {
                statusText.text = "Player " + (gameBoard[0, 2] == GridState.X ? "X" : "O") + " wins!";
                return true;
            }
            // Check for a tie game
            bool isTie = true;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (gameBoard[i, j] == GridState.Empty)
                    {
                        isTie = false;
                        break;
                    }
                }
            }
            if (isTie)
            {
                statusText.text = "Tie game!";
                return true;
            }

            return false;
        }

        [BurstCompile]
        private struct UpdateGameBoardJobParallelFor : IJobParallelFor
        {
            public int column;
            public bool isXTurn;
            public NativeArray<GridState> gameBoard;

            public void Execute(int row)
            {
                int index = row * 3 + column;
                if (gameBoard[index] == GridState.Empty)
                {
                    gameBoard[index] = isXTurn ? GridState.X : GridState.O;
                }
            }
        }



        private void HandleButtonClick(int row, int column)
        {
            if (isGameEnded)
            {
                return;
            }
            if (gameBoard[row, column] != GridState.Empty)
            {
                return;
            }

            NativeArray<GridState> nativeGameBoard = new NativeArray<GridState>(9, Allocator.TempJob);
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    int index = i * 3 + j;
                    nativeGameBoard[index] = gameBoard[i, j];
                }
            }

            var job = new UpdateGameBoardJobParallelFor
            {
                column = column,
                isXTurn = isXTurn,
                gameBoard = nativeGameBoard,
            };
            var handle = job.Schedule(1, 1);//3,1
            handle.Complete();

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    int index = i * 3 + j;
                    gameBoard[i, j] = nativeGameBoard[index];
                }
            }

            nativeGameBoard.Dispose();

            UpdateBoardDisplay();

            if (CheckForWinner())
            {
                isGameEnded = true;
                return;
            }

            isXTurn = !isXTurn;
            statusText.text = "Player " + (isXTurn ? "X" : "O") + "'s turn";
        }




        private void SetButtonListeners()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    int row = i;
                    int column = j;
                    int index = row * 3 + column;
                    gridButtons[index].onClick.AddListener(() => HandleButtonClick(row, column));
                }
            }
        }

        private void OnEnable()
        {
            SetButtonListeners();
        }
    }

}
*/

/*
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;
using System.Diagnostics;
using System;

namespace ParallelJobs_MonteCarmo_MiniMax
{
    struct MCTSJob : IJobParallelFor
    {
        public int iterationNumber;
        [ReadOnly] public NativeArray<char[]> boardStates;
        [WriteOnly] public NativeArray<double> uctValues;

        public void Execute(int i)
        {
            State state = new State(boardStates[i], Board.TURN_O, new Point(-1, -1), new Point(-1, -1), 0);
            TreeNode tn = new TreeNode(state);
            tn.iterateMCTS();
            double bestUCTValue = double.MinValue;
            foreach (TreeNode child in tn.children)
            {
                int lastPosX = child.state.lastPos.x;
                int lastPosY = child.state.lastPos.y;
                uctValues[lastPosX * Board.BOARD_SIZE + lastPosY] = child.uctValue;
                if (child.uctValue > bestUCTValue)
                {
                    bestUCTValue = child.uctValue;
                }
            }
        }
    }

    public class MCTSAI : MonoBehaviour
    {
        public static char myTurn = Board.TURN_X;
        public Board board;
        public int iterationNumber;
        [HideInInspector] public TreeNode tn;
        [HideInInspector] public double[][] uctValues;
        private JobHandle jobHandle;
        private NativeArray<char[]> boardStates;
        private NativeArray<double> uctValuesNativeArray;

        //public void initAI() { }
        public void Start()//initAI()
        {
            tn = new TreeNode(new State(board.boardState, board.currentTurn, board.lastPos, board.lastOPos, board.pieceNumber));
            uctValues = new double[Board.BOARD_SIZE][];
            for (int i = 0; i < uctValues.Length; i++)
            {
                uctValues[i] = new double[Board.BOARD_SIZE];
                for (int j = 0; j < uctValues[i].Length; j++)
                {
                    uctValues[i][j] = double.MinValue;
                }
            }
            boardStates = new NativeArray<char[]>(iterationNumber, Allocator.Persistent);
            uctValuesNativeArray = new NativeArray<double>(Board.BOARD_SIZE * Board.BOARD_SIZE, Allocator.Persistent);
        }

        public int BatchSize = 64;
        void Update()
        {
            if (board.isStarted && board.currentTurn == myTurn && board.result == Board.RESULT_NONE)
            {
                bool flag = false;
                if (tn.children.Count > 0)
                {
                    foreach (TreeNode child in tn.children)
                    {
                        if ((child.state.lastPos.isEqual(board.lastPos))
                            && (child.state.lastOPos.isEqual(board.lastOPos)))
                        {
                            tn = child;
                            flag = true;
                            break;
                        }
                    }
                    if (!flag) UnityEngine.Debug.Log("unreachable code");

                }
                else
                {
                    tn = new TreeNode(new State(board.boardState, board.currentTurn, board.lastPos, board.lastOPos, board.pieceNumber));
                }

                var watch = Stopwatch.StartNew();
                for (int i = 0; i < iterationNumber; i++)
                {
                    boardStates[i] = Util.deepcloneArray(board.boardState);
                }
                var mctsJob = new MCTSJob
                {
                    iterationNumber = iterationNumber,
                    boardStates = boardStates,
                    uctValues = uctValuesNativeArray
                };
                jobHandle = mctsJob.Schedule(iterationNumber, BatchSize);
                jobHandle.Complete();
                watch.Stop();

                // Update uctValues from the native array
                for (int i = 0; i < Board.BOARD_SIZE; i++)
                {
                    for (int j = 0; j < Board.BOARD_SIZE; j++)
                    {
                        uctValues[i][j] = uctValuesNativeArray[i * Board.BOARD_SIZE + j];
                    }
                }

                tn = tn.select();
                updateUCTValues();
                if (myTurn == Board.TURN_X)
                {
                    board.selectSquare(tn.state.lastPos.x, tn.state.lastPos.y);
                }
                else
                {
                    board.selectSquare(tn.state.lastOPos.x, tn.state.lastOPos.y);
                }
            }
        }

        internal void initAI()
        {
            //throw new NotImplementedException();
        }

        void updateUCTValues()
        {
            foreach (TreeNode child in tn.children)
            {
                int lastPosX = myTurn == Board.TURN_X ? child.state.lastOPos.x : child.state.lastPos.x;
                int lastPosY = myTurn == Board.TURN_X ? child.state.lastOPos.y : child.state.lastPos.y;
                uctValues[lastPosX][lastPosY] = child.uctValue;
            }
        }

        private void OnDestroy()
        {
            jobHandle.Complete();
            boardStates.Dispose();
            uctValuesNativeArray.Dispose();
        }
    }
}
*/



using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;
using System.Diagnostics;
using System;

namespace ParallelJobs_MonteCarmo_MiniMax
{
    [BurstCompile]
    struct MCTSJob : IJobParallelFor
    {
        public int iterationNumber;
        public TreeNode tn;

        public void Execute(int i)
        {
            tn.iterateMCTS();
        }
    }

    public class MCTSAI : MonoBehaviour
    {
        public static char myTurn = Board.TURN_X;
        public Board board;
        public int iterationNumber;
        [HideInInspector] public TreeNode tn;
        [HideInInspector] public double[][] uctValues;
        private JobHandle jobHandle;

        //public void initAI() { }
        public void Start()//initAI()
        {
            tn = new TreeNode(new State(board.boardState, board.currentTurn, board.lastPos, board.lastOPos, board.pieceNumber));
            uctValues = new double[Board.BOARD_SIZE][];
            for (int i = 0; i < uctValues.Length; i++)
            {
                uctValues[i] = new double[Board.BOARD_SIZE];
                for (int j = 0; j < uctValues[i].Length; j++)
                {
                    uctValues[i][j] = double.MinValue;
                }
            }
        }

        public int BatchSize = 64;
        void Update()
        {
            if (board.isStarted && board.currentTurn == myTurn && board.result == Board.RESULT_NONE)
            {
                bool flag = false;
                if (tn.children.Count > 0)
                {
                    foreach (TreeNode child in tn.children)
                    {
                        if ((child.state.lastPos.isEqual(board.lastPos))
                            && (child.state.lastOPos.isEqual(board.lastOPos)))
                        {
                            tn = child;
                            flag = true;
                            break;
                        }
                    }
                    if (!flag) UnityEngine.Debug.Log("unreachable code");

                }
                else
                {
                    tn = new TreeNode(new State(board.boardState, board.currentTurn, board.lastPos, board.lastOPos, board.pieceNumber));
                }

                var watch = Stopwatch.StartNew();
                MCTSJob mctsJob = new MCTSJob
                {
                    iterationNumber = iterationNumber,
                    tn = tn
                };
                jobHandle = mctsJob.Schedule(iterationNumber, BatchSize);
                jobHandle.Complete();
                watch.Stop();

                tn = tn.select();
                updateUCTValues();
                if (myTurn == Board.TURN_X)
                {
                    board.selectSquare(tn.state.lastPos.x, tn.state.lastPos.y);
                }
                else
                {
                    board.selectSquare(tn.state.lastOPos.x, tn.state.lastOPos.y);
                }
            }
        }

        internal void initAI()
        {
            //throw new NotImplementedException();
        }

        void updateUCTValues()
        {
            foreach (TreeNode child in tn.children)
            {
                int lastPosX = myTurn == Board.TURN_X ? child.state.lastOPos.x : child.state.lastPos.x;
                int lastPosY = myTurn == Board.TURN_X ? child.state.lastOPos.y : child.state.lastPos.y;
                uctValues[lastPosX][lastPosY] = child.uctValue;
            }
        }

        private void OnDestroy()
        {
            jobHandle.Complete();
        }
    }
}