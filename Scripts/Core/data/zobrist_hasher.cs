using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class zobrist_hasher
{
    public static ulong positionHash;

    static ulong[,,] positionalNumbers = new ulong[8, 8, 12];
    static ulong whiteToMove;
    static ulong whiteQueensite;
    static ulong whiteKingsite;
    static ulong blackQueensite;
    static ulong blackKingsite;
    static ulong[] enPassantSquares = new ulong[8];

    // just a random seed I typed out and seems to work
    const int seed = 902756003;
    static System.Random numberGenerator = new System.Random(seed);

    public static void Initialize()
    {
        // initializing our zobrist hasher by associating a random 64 bit int to everything
        for (int i = 0, n = positionalNumbers.GetLength(0); i < n; i++)
        {
            for (int j = 0, o = positionalNumbers.GetLength(1); j < o; j++)
            {
                for (int k = 0, p = positionalNumbers.GetLength(2); k < p; k++)
                {
                    positionalNumbers[j, i, k] = GetRandomUlong();
                }
            }
        }

        whiteToMove = GetRandomUlong();
        whiteQueensite = GetRandomUlong();
        whiteKingsite = GetRandomUlong();
        blackQueensite = GetRandomUlong();
        blackKingsite = GetRandomUlong();

        for (int i = 0, n = enPassantSquares.Length; i < n; i++)
        {
            enPassantSquares[i] = GetRandomUlong();
        }
    }

    public static ulong GetPositionHash(gameState game)
    {
        // to get the hash of a position we then xor every random number that
        // is true in the current position together
        ulong hash = 0;

        for (int i = 0, n = game.pieces.GetLength(0); i < n; i++)
        {
            for (int j = 0, o = game.pieces.GetLength(1); j < o; j++)
            {
                if (game.pieces[j, i].type != board.nothing)
                {
                    hash ^= GetPieceHash(new Vector2Int(j, i), game.pieces[j, i]);
                }
            }
        }

        if (game.whitesTurn)
        {
            hash ^= whiteToMove;
        }
        if (game.whiteQueensite)
        {
            hash ^= whiteQueensite;
        }
        if (game.whiteKingsite)
        {
            hash ^= whiteKingsite;
        }
        if (game.blackQueensite)
        {
            hash ^= blackQueensite;
        }
        if (game.blackKingsite)
        {
            hash ^= blackKingsite;
        }

        if (game.enPassantSquares.Count != 0)
        {
            hash ^= enPassantSquares[game.enPassantSquares[0].x];
        }

        return hash;
    }

    public static ulong GetHashAfter(move move, gameState game)
    {
        // the same can be simply updated by first removing the piece from
        // its current square and then xoring it to the target square
        ulong hash = positionHash;

        // updating en passant squares and castling rights
        if (game.enPassantSquares.Count != 0) { hash ^= enPassantSquares[game.enPassantSquares[0].x]; }

        if (game.whiteKingsite && (move.startSquare == new Vector2Int(7, 7) || move.endSquare == new Vector2Int(7, 7)))
        {
            hash ^= whiteKingsite;
        }
        if (game.whiteQueensite && (move.startSquare == new Vector2Int(0, 7) || move.endSquare == new Vector2Int(0, 7)))
        {
            hash ^= whiteQueensite;
        }
        if (move.startSquare == new Vector2Int(4, 7))
        {
            if (game.whiteKingsite) { hash ^= whiteKingsite; }
            if (game.whiteQueensite) { hash ^= whiteQueensite; }
        }

        if (game.blackKingsite && (move.startSquare == new Vector2Int(7, 0) || move.endSquare == new Vector2Int(7, 0)))
        {
            hash ^= blackKingsite;
        }
        if (game.blackQueensite && (move.startSquare == new Vector2Int(0, 0) || move.endSquare == new Vector2Int(0, 0)))
        {
            hash ^= blackQueensite;
        }
        if (move.startSquare == new Vector2Int(4, 0))
        {
            if (game.blackKingsite) { hash ^= blackKingsite; }
            if (game.blackQueensite) { hash ^= blackQueensite; }
        }

        if (game.pieces[move.startSquare.x, move.startSquare.y].type == board.pawn)
        {
            if (move.startSquare.y == 1 && move.endSquare.y == 3)
            {
                hash ^= enPassantSquares[move.startSquare.x];
            }
            if (move.startSquare.y == 6 && move.endSquare.y == 4)
            {
                hash ^= enPassantSquares[move.startSquare.x];
            }
        }

        // moving the piece normally
        hash ^= GetPieceHash(move.startSquare, game.pieces[move.startSquare.x, move.startSquare.y]);
        
        if (game.pieces[move.endSquare.x, move.endSquare.y].type != board.nothing)
        {
            hash ^= GetPieceHash(move.endSquare, game.pieces[move.endSquare.x, move.endSquare.y]);
        }
        hash ^= GetPieceHash(move.endSquare, game.pieces[move.startSquare.x, move.startSquare.y]);


        // and updating the hash for special moves
        if (move.isSpecialMove)
        {
            if (move.isPiece)
            {
                if (move.specialMovePiece.type != board.nothing)
                {
                    hash ^= GetPieceHash(move.specialMoveTarget, game.pieces[move.startSquare.x, move.startSquare.y]);
                    hash ^= GetPieceHash(move.specialMoveTarget, move.specialMovePiece);
                }
                else
                {
                    hash ^= GetPieceHash(move.specialMoveTarget, game.pieces[move.specialMoveTarget.x, move.specialMoveTarget.y]);
                }
            }
            else
            {
                hash ^= GetPieceHash(move.specialMoveStart, game.pieces[move.specialMoveStart.x, move.specialMoveStart.y]);

                if (game.pieces[move.specialMoveTarget.x, move.specialMoveTarget.y].type != board.nothing)
                {
                    hash ^= GetPieceHash(move.specialMoveTarget, game.pieces[move.specialMoveTarget.x, move.specialMoveTarget.y]);
                }
                hash ^= GetPieceHash(move.specialMoveTarget, game.pieces[move.specialMoveStart.x, move.specialMoveStart.y]);
            }
        }

        hash ^= whiteToMove;

        return hash;
    }

    // getting the hash of a piece on a specified square using our array
    static ulong GetPieceHash(Vector2Int index, piece piece)
    {
        int pieceIndex = (piece.type - 1) + (piece.isWhite ? 0 : 6);
        return positionalNumbers[index.x, index.y, pieceIndex];
    }

    // getting a random 64 bit number
    static ulong GetRandomUlong()
    {
        byte[] buffer = new byte[8];
        numberGenerator.NextBytes(buffer);

        return BitConverter.ToUInt64(buffer);
    }
}
