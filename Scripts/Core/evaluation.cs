using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System;
using System.Threading;

public static class evaluation
{
    public const int positiveInfinity = int.MaxValue / 2;
    public const int negativeInfinity = int.MinValue / 2;

    public static float currentEndgameWeight = 0;

    public static move bestMoveInPosition;

    public static bool searchRunning = true;
    public static Stopwatch timeMeasurement;
    public static TimeSpan timeSpent;
    public static int searchTime;

    public static List<move> bestPlayedMoves = new List<move>();

    public static transposition_table table;
    public const int tableSize = 64000000;

    public static void Initialize()
    {
        zobrist_hasher.Initialize();
    }

    public static void Clear()
    {
        zobrist_hasher.positionHash = zobrist_hasher.GetPositionHash(board.game);

        table = new transposition_table(tableSize);
    }

    public static move GetBestMove(int timeInSeconds)
    {
        // doing iterative deepening until our time limit is reached

        timeMeasurement = new Stopwatch();
        timeMeasurement.Start();
        searchTime = timeInSeconds;

        currentEndgameWeight = GetEndgameWeight(board.game);
        bestMoveInPosition = new move();
        searchRunning = true;

        bestPlayedMoves.Clear();
        
        for (int searchDepth = 1; searchDepth < int.MaxValue; searchDepth++)
        {
            if (!searchRunning)
            {
                break;
            }

            int score = Search(negativeInfinity, positiveInfinity, searchDepth, 0);
            logger.Log("Best move: " + bestMoveInPosition.startSquare + " " + bestMoveInPosition.endSquare + " Depth: " + searchDepth + " Evaluation: " + score);
        }

        return bestMoveInPosition;
    }

    public static int Search(int alpha, int beta, int depth, int rootDistance)
    {
        // if we exceed our given think time, we quit
        timeSpent = timeMeasurement.Elapsed;
        if (timeSpent.Seconds > searchTime)
        {
            searchRunning = false;
            return 0;
        }

        // checking if we have a draw through insufficent material or repetition
        if (rootDistance > 0)
        {
            if (board.RepetitionTableContains(zobrist_hasher.positionHash) || board_helper.GetNumPieces(board.game) == 2)
            {
                return 0;
            }
        }

        // if we have reached out search depth we return the score of the current position
        if (depth == 0)
        {
            return Quiescence(alpha, beta);
        }

        // generating all pseudo legal moves
        List<move> legalMoves = move_generator.GeneratePseudoLegalMoves(board.game);
        int illegalMovesCount = 0;

        // ordering moves for speed improvement
        legalMoves = OrderMoves(legalMoves);

        gameState storedGameState = new gameState(board.game);

        move bestLocalMove = new move();
        int bestEvaluation = negativeInfinity;

        ulong storedHash = zobrist_hasher.positionHash;

        // getting the stored data from the transposition table for our current position using the zobrist hasher
        transposition_table.entry storedEntry = table.Get(storedHash);

        // we should be able to use the hasher without having to look up if the move is inside the pseudo legal moves
        // but there is still a chance of one in 4 billion, so we still check
        bool isValid = storedEntry.valid && legalMoves.Contains(storedEntry.bestMove);
        byte nodeType = transposition_table.alphaValue;
        
        if (isValid)
        {
            if (storedEntry.searchDepth >= depth)
            {
                // if have stored the value from a full search we can simply return it
                if (storedEntry.nodeType == transposition_table.exactValue)
                {
                    bestLocalMove = storedEntry.bestMove;
                    if (rootDistance == 0)
                    {
                        bestMoveInPosition = bestLocalMove;
                    }

                    return storedEntry.evaluation;
                }
                // we have stored the upper bound
                // if it is less than alpha the moves won't interest us, otherwise we need to search them
                if (storedEntry.nodeType == transposition_table.alphaValue && storedEntry.evaluation <= alpha)
                {
                    return storedEntry.evaluation;
                }
                // we have stored the lower bound
                // we only use it if it causes a cutt off
                if (storedEntry.nodeType == transposition_table.betaValue && storedEntry.evaluation >= beta)
                {
                    return storedEntry.evaluation;
                }
            }
            // if we can't use the score we still can search the best move first, because we might agree with it being
            // the best one
            legalMoves.Remove(storedEntry.bestMove);
            legalMoves.Insert(0, storedEntry.bestMove);
        }
        
        // we play every possible move in the current position and check if that would leave us in check
        foreach (move move in legalMoves)
        {
            board.MakeMove(move);

            if (!board_helper.IsInCheck((storedGameState.whitesTurn ? board.game.whiteKingSquare : board.game.blackKingSquare), board.game, storedGameState.whitesTurn))
            {
                // recursively callind ouselfes and inversing the result:
                // an evaluation that is bad for us is good for our opponent and vice versa
                int evaluation = -Search(-beta, -alpha, depth - 1, rootDistance + 1);

                // quitting the search here aswell, because this is a recursive function
                if (!searchRunning)
                {
                    board.UnmakeMove(new gameState(storedGameState), storedHash);
                    return 0;
                }

                // checking if we exceed current alpha and if we do the evaluation
                // is no longer a upper bound
                if (evaluation > alpha)
                {
                    alpha = evaluation;
                    bestLocalMove = move;
                    nodeType = transposition_table.exactValue;
                }

                // we can do an alpha beta cutoff because our opponent would simply play another move
                if (evaluation >= beta)
                {
                    table.Store(storedHash, bestLocalMove, depth, beta, transposition_table.betaValue);
                    board.UnmakeMove(new gameState(storedGameState), storedHash);
                    return beta;
                }
            }
            else
            {
                illegalMovesCount++;
            }

            board.UnmakeMove(new gameState(storedGameState), storedHash);
        }

        // setting the best possible move globally, because this is the root node
        if (rootDistance == 0)
        {
            bestMoveInPosition = bestLocalMove;
        }

        // we didn't found a legal move, so it either needs to be stalemate or checkmate
        if (illegalMovesCount == legalMoves.Count)
        {
            if (board_helper.IsInCheck((board.game.whitesTurn ? board.game.whiteKingSquare : board.game.blackKingSquare), board.game, board.game.whitesTurn))
            {
                return negativeInfinity + rootDistance + 1;
            }
            return 0;
        }

        // storing the result if it isn't a checkmate, because that would lead to errors with
        // us not actually playing the checkmate
        if (Mathf.Abs(bestEvaluation) < positiveInfinity - 100)
        {
            table.Store(storedHash, bestLocalMove, depth, alpha, nodeType);
        }

        // returning the score
        return alpha;
    }

    static int Quiescence(int alpha, int beta)
    {
        // searching all captures in oder to get rid of the "horizon effect"
        // a very similar concept to the above 

        int evaluation = EvaluateMaterial(board.game);
        if (evaluation >= beta)
        {
            return beta;
        }
        if (evaluation > alpha)
        {
            alpha = evaluation;
        }

        List<move> captures = move_generator.GeneratePseudoLegalMoves(board.game, generateNonCaptures: false);
        captures = OrderMoves(captures);

        gameState storedGameState = new gameState(board.game);

        ulong storedHash = zobrist_hasher.positionHash;

        foreach (move m in captures)
        {
            board.MakeMove(m);
            if (!board_helper.IsInCheck((storedGameState.whitesTurn ? board.game.whiteKingSquare : board.game.blackKingSquare), board.game, storedGameState.whitesTurn))
            {
                evaluation = -Quiescence(-beta, -alpha);

                if (evaluation >= beta)
                {
                    board.UnmakeMove(new gameState(storedGameState), storedHash);
                    return beta;
                }
                if (evaluation > alpha)
                {
                    alpha = evaluation;
                }
            }

            board.UnmakeMove(new gameState(storedGameState), storedHash);
        }

        return alpha;
    }

    static List<move> OrderMoves(List<move> legalMoves)
    {
        // ordering the moves in order for alpha beta pruning to be more effective

        sorter sort = new sorter();
        legalMoves.Sort(sort);
        legalMoves.Reverse();
        
        return legalMoves;
    }

    public static int EvaluateMaterial(gameState game)
    {
        // calculating the material balance and then putting it into perspective to whose turn it is

        float endgameWeight = GetEndgameWeight(game);

        int whiteEval = GetMaterial(game, true, endgameWeight);
        int blackEval = GetMaterial(game, false, endgameWeight);
        int whiteMopUp = MopUpEvaluation(game, whiteEval, blackEval, endgameWeight, true);
        int blackMopUp = MopUpEvaluation(game, blackEval, whiteEval, endgameWeight, false);

        int evaluation = (whiteEval + whiteMopUp) - (blackEval + blackMopUp);
        int perspective = (game.whitesTurn) ? 1 : -1;

        return evaluation * perspective;
    }
    
    static int MopUpEvaluation(gameState game, int myEval, int enemyEval, float endgameWeight, bool isWhite)
    {
        // a simple mop up bonus in order to help checkmating in the endgame

        if (myEval > enemyEval + values.pawnValue * 2 && endgameWeight > 0)
        {
            Vector2Int opponentPosition = isWhite ? game.blackKingSquare : game.whiteKingSquare;
            Vector2Int kingPosition = isWhite ? game.whiteKingSquare : game.blackKingSquare;

            int centerDistance = values.centerDistance[board_helper.GetTableIndex(opponentPosition, !isWhite)];

            int kingDistance = Mathf.Abs(kingPosition.x - opponentPosition.x) + Mathf.Abs(kingPosition.y + opponentPosition.y);

            float evaluation = 4.7f * centerDistance + 1.6f * (14 - kingDistance);

            return (int)Mathf.Round(evaluation * 10 * endgameWeight);
        }
        return 0;
    }

    static float GetEndgameWeight(gameState game)
    {
        // some things, like pushing pawn are a lot more important in the endgame
		// so we use an endgame weight in order to lerp between the bonuses

        int piecesCount = board_helper.CountNonPawns(game);
        if (piecesCount < 7)
        {
            return (1 - (piecesCount / 7));
        }
        return 0;
    }

    static int GetMaterial(gameState game, bool isWhite, float endgameWeight)
    {
        int material = 0;

        for (int i = 0, n = (isWhite ? game.whiteSquares.Count : game.blackSquares.Count); i < n; i++)
        {
            Vector2Int index = (isWhite ? game.whiteSquares[i] : game.blackSquares[i]);
            material += GetPieceValue(game.pieces[index.x, index.y], index, endgameWeight);
        }
        return material;
    }

    public static int GetPieceValue(piece piece, Vector2Int index, float endgameWeight)
    {
        switch (piece.type)
        {
            case board.queen:
                return values.queenValue + values.queenSquares[board_helper.GetTableIndex(index, piece.isWhite)];
            case board.rook:
                return values.rookValue + values.rookSquares[board_helper.GetTableIndex(index, piece.isWhite)];
            case board.bishop:
                return values.bishopValue + values.bishopSquares[board_helper.GetTableIndex(index, piece.isWhite)];
            case board.knight:
                return values.knightValue + values.knightSquares[board_helper.GetTableIndex(index, piece.isWhite)];
            case board.pawn:
                return values.pawnValue + (int)Mathf.Lerp(values.pawnSquares[board_helper.GetTableIndex(index, piece.isWhite)], values.endgamePawnSquares[board_helper.GetTableIndex(index, piece.isWhite)], endgameWeight);
            case board.king:
                return values.kingValue + (int)Mathf.Lerp(values.kingSquares[board_helper.GetTableIndex(index, piece.isWhite)], values.kingEndgameSquares[board_helper.GetTableIndex(index, piece.isWhite)], endgameWeight);
            default:
                return 0;
        }
    }

    public static int GetMoveScore(move move, gameState game, float endgameWeight)
    {
        int moveScore = 0;
        piece movePiece = game.pieces[move.startSquare.x, move.startSquare.y];
        piece capturePiece = game.pieces[move.endSquare.x, move.endSquare.y];

        if (capturePiece.type != board.nothing)
        {
            moveScore = 10 * GetPieceValue(capturePiece, move.endSquare, endgameWeight) - GetPieceValue(movePiece, move.startSquare, endgameWeight);
        }

        if (movePiece.type == board.pawn && move.isSpecialMove && move.isPiece && move.specialMovePiece.type != board.nothing)
        {
            moveScore += GetPieceValue(move.specialMovePiece, move.specialMoveTarget, endgameWeight);
        }

        return moveScore;
    }

    public static int Perft(int depth)
    {
        // a simple function for running a perft to check the move genrator

        if (depth == 0)
        {
            return 1;
        }

        List<move> legalMoves = move_generator.GeneratePseudoLegalMoves(board.game);
        int numPositions = 0;
        gameState storedGameState = new gameState(board.game);
        ulong storedHash = zobrist_hasher.positionHash;

        foreach (move move in legalMoves)
        {
            board.MakeMove(move);
            if (!board_helper.IsInCheck((storedGameState.whitesTurn ? board.game.whiteKingSquare : board.game.blackKingSquare), board.game, storedGameState.whitesTurn))
            {
                numPositions += Perft(depth - 1);
            }
            board.UnmakeMove(new gameState(storedGameState), storedHash);
        }

        return numPositions;
    }
}

public class sorter : IComparer<move>
{
    public int Compare (move x, move y)
    {
        int xValue = evaluation.GetMoveScore(x, board.game, evaluation.currentEndgameWeight);
        int yValue = evaluation.GetMoveScore(y, board.game, evaluation.currentEndgameWeight);

        return xValue.CompareTo(yValue);
    }
}
