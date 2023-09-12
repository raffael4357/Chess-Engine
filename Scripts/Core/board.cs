using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class board
{
    public const int nothing = 0;
    public const int king = 1;
    public const int queen = 2;
    public const int rook = 3;
    public const int bishop = 4;
    public const int knight = 5;
    public const int pawn = 6;

    public const string startFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq -";

    public static gameState game;

    public static List<ulong> repetitionTable = new List<ulong>();

    public static void SetStart()
    {
        // setting the game state back to the specified start fen
        game = TransformFEN(startFEN);
        game.blackSquares = board_helper.GetPieceSquares(game, false);
        game.whiteSquares = board_helper.GetPieceSquares(game, true);
        game.whiteKingSquare = board_helper.GetKingIndex(game, game.whiteSquares);
        game.blackKingSquare = board_helper.GetKingIndex(game, game.blackSquares);

        // and obviously clearing the repetition table
        repetitionTable.Clear();
    }

    public static void MakeMove(move input)
    {
        // making a move on the board
        // first setting the new hash of the zobrist hasher
        zobrist_hasher.positionHash = zobrist_hasher.GetHashAfter(input, game);

        // and then updating the stored piece squares
        // which are needed for us not looping over the whole board constantly
        if (game.pieces[input.startSquare.x, input.startSquare.y].isWhite)
        {
            game.whiteSquares.Remove(input.startSquare);
            game.whiteSquares.Add(input.endSquare);

            if (game.pieces[input.startSquare.x, input.startSquare.y].type == king)
            {
                game.whiteKingSquare = input.endSquare;
            }
            if (game.pieces[input.endSquare.x, input.endSquare.y].type != nothing)
            {
                game.blackSquares.Remove(input.endSquare);
            }

            if (input.isSpecialMove)
            {
                if (input.isPiece)
                {
                    if (input.specialMovePiece.type == nothing)
                    {
                        game.blackSquares.Remove(input.specialMoveTarget);
                    }
                }
                else
                {
                    game.whiteSquares.Remove(input.specialMoveStart);
                    game.whiteSquares.Add(input.specialMoveTarget);
                }
            }
        }
        else
        {
            game.blackSquares.Remove(input.startSquare);
            game.blackSquares.Add(input.endSquare);

            if (game.pieces[input.startSquare.x, input.startSquare.y].type == king)
            {
                game.blackKingSquare = input.endSquare;
            }
            if (game.pieces[input.endSquare.x, input.endSquare.y].type != nothing)
            {
                game.whiteSquares.Remove(input.endSquare);
            }

            if (input.isSpecialMove)
            {
                if (input.isPiece)
                {
                    if (input.specialMovePiece.type == nothing)
                    {
                        game.whiteSquares.Remove(input.specialMoveTarget);
                    }
                }
                else
                {
                    game.blackSquares.Remove(input.specialMoveStart);
                    game.blackSquares.Add(input.specialMoveTarget);
                }
            }
        }

        // then setting the en passant squares and castling rights
        game.enPassantSquares.Clear();

        if (input.startSquare == new Vector2Int(7, 7) || input.endSquare == new Vector2Int(7, 7))
        {
            game.whiteKingsite = false;
        }
        if (input.startSquare == new Vector2Int(0, 7) || input.endSquare == new Vector2Int(0, 7))
        {
            game.whiteQueensite = false;
        }
        if (input.startSquare == new Vector2Int(4, 7))
        {
            game.whiteKingsite = false;
            game.whiteQueensite = false;
        }

        if (input.startSquare == new Vector2Int(7, 0) || input.endSquare == new Vector2Int(7, 0))
        {
            game.blackKingsite = false;
        }
        if (input.startSquare == new Vector2Int(0, 0) || input.endSquare == new Vector2Int(0, 0))
        {
            game.blackQueensite = false;
        }
        if (input.startSquare == new Vector2Int(4, 0))
        {
            game.blackKingsite = false;
            game.blackQueensite = false;
        }

        if (game.pieces[input.startSquare.x, input.startSquare.y].type == pawn)
        {
            if (input.startSquare.y == 1 && input.endSquare.y == 3)
            {
                game.enPassantSquares.Add(new Vector2Int(input.startSquare.x, 2));
            }
            if (input.startSquare.y == 6 && input.endSquare.y == 4)
            {
                game.enPassantSquares.Add(new Vector2Int(input.startSquare.x, 5));
            }
        }

        // lastly first moving the piece normally
        game.pieces[input.endSquare.x, input.endSquare.y] = game.pieces[input.startSquare.x, input.startSquare.y];
        game.pieces[input.startSquare.x, input.startSquare.y].type = nothing;
        game.whitesTurn = !game.whitesTurn;

        // and then executing any specified special moves
        if (input.isSpecialMove)
        {
            if (input.isPiece)
            {
                game.pieces[input.specialMoveTarget.x, input.specialMoveTarget.y] = input.specialMovePiece;
            }
            else
            {
                game.pieces[input.specialMoveTarget.x, input.specialMoveTarget.y] = game.pieces[input.specialMoveStart.x, input.specialMoveStart.y];
                game.pieces[input.specialMoveStart.x, input.specialMoveStart.y].type = board.nothing;
            }
        }
    }

    public static void UnmakeMove(gameState stored, ulong storedHash)
    {
        // unmaking a move by simply setting the game to the stored state
        game = stored;
        zobrist_hasher.positionHash = storedHash;
    }

    public static gameState TransformFEN(string FEN)
    {
        // transforming a fen into a state of the game
        gameState position = new gameState();
        position.pieces = new piece[8, 8];

        // first the pieces
        Vector2Int index = new Vector2Int(0, 0);
        int i;
        for (i = 0; !Char.IsWhiteSpace(FEN[i]); i++)
        {
            if (FEN[i] == '/')
            {
                index.y++;
                index.x = 0;
                continue;
            }

            if (Char.IsDigit(FEN[i]))
            {
                int number = int.Parse(FEN[i].ToString());
                for (int j = 0; j < number; j++)
                {
                    position.pieces[index.x, index.y].type = nothing;
                    index.x++;
                }
            }
            else
            {
                position.pieces[index.x, index.y] = board_helper.GetPiece(FEN[i]);
                index.x++;
            }
        }
        // then whose turn it is
        if (FEN[i + 1] == 'w')
        {
            position.whitesTurn = true;
        }
        else
        {
            position.whitesTurn = false;
        }

        // afterwards the castling rights
        for (i += 3; !Char.IsWhiteSpace(FEN[i]); i++)
        {
            if (FEN[i] == 'K')
            {
                position.whiteKingsite = true;
            }
            else if (FEN[i] == 'Q')
            {
                position.whiteQueensite = true;
            }
            else if (FEN[i] == 'k')
            {
                position.blackKingsite = true;
            }
            else if (FEN[i] == 'q')
            {
                position.blackQueensite = true;
            }
        }

        // and then en passant squares
        position.enPassantSquares = new List<Vector2Int>();
        if (FEN[i + 1] != '-')
        {
            string square = FEN.Substring(i + 1, 2);
            Vector2Int enPassantSquare = board_helper.TransformNotation(square);

            position.enPassantSquares.Add(enPassantSquare);
        }

        return position;
    }

    public static void AddToRepetitionTable(ulong hash)
    {
        // adding a position into our repetition table, but by order
        int index = repetitionTable.BinarySearch(hash);
        if (index < 0)
        {
            index = ~index;
        }

        repetitionTable.Insert(index, hash);
    }

    public static bool RepetitionTableContains(ulong hash)
    {
        // searching our repetition table for the specified position
        // using binary search, since we keep our list sorted at all times
        int index = repetitionTable.BinarySearch(hash);
        if (index < 0)
        {
            return false;
        }
        return true;
    }
}

// the structs for pieces
public struct piece
{
    public int type;
    public bool isWhite;

    public piece(int piece, bool white)
    {
        type = piece;
        isWhite = white;
    }
    public override bool Equals(object obj)
    {
        if (obj == null)
        {
            return false;
        }
        if (!(obj is piece))
        {
            return false;
        }
        return ((piece)obj).type == this.type && ((piece)obj).isWhite == this.isWhite;
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

// states of the game
public struct gameState
{
    public piece[,] pieces;
    public bool whitesTurn;
    public List<move> legalMoves;

    public bool whiteQueensite;
    public bool whiteKingsite;
    public bool blackQueensite;
    public bool blackKingsite;

    public List<Vector2Int> enPassantSquares;

    public List<Vector2Int> whiteSquares;
    public List<Vector2Int> blackSquares;
    public Vector2Int blackKingSquare;
    public Vector2Int whiteKingSquare;

    public gameState(gameState root)
    {
        pieces = new piece[8, 8];
        for (int i = 0, n = root.pieces.GetLength(0); i < n; i++)
        {
            for (int j = 0, o = root.pieces.GetLength(1); j < o; j++)
            {
                pieces[j, i] = root.pieces[j, i];
            }
        }

        whitesTurn = root.whitesTurn;

        legalMoves = new List<move>();
        if (root.legalMoves != null)
        {
            for (int i = 0, n = root.legalMoves.Count; i < n; i++)
            {
                legalMoves.Add(root.legalMoves[i]);
            }
        }

        whiteKingsite = root.whiteKingsite;
        whiteQueensite = root.whiteQueensite;
        blackKingsite = root.blackKingsite;
        blackQueensite = root.blackQueensite;

        enPassantSquares = new List<Vector2Int>();
        if (root.enPassantSquares != null)
        {
            for (int i = 0, n = root.enPassantSquares.Count; i < n; i++)
            {
                enPassantSquares.Add(root.enPassantSquares[i]);
            }
        }

        blackKingSquare = root.blackKingSquare;
        whiteKingSquare = root.whiteKingSquare;

        whiteSquares = new List<Vector2Int>();
        if (root.whiteSquares != null)
        {
            for (int i = 0, n = root.whiteSquares.Count; i < n; i++)
            {
                whiteSquares.Add(root.whiteSquares[i]);
            }
        }

        blackSquares = new List<Vector2Int>();
        if (root.blackSquares != null)
        {
            for (int i = 0, n = root.blackSquares.Count; i < n; i++)
            {
                blackSquares.Add(root.blackSquares[i]);
            }
        }
    }
}

// and moves
public struct move
{
    public Vector2Int startSquare;
    public Vector2Int endSquare;

    public bool isSpecialMove;
    public Vector2Int specialMoveTarget;

    public bool isPiece;
    public piece specialMovePiece;
    public Vector2Int specialMoveStart;

    public override bool Equals(object obj)
    {
        if (obj == null)
        {
            return false;
        }
        if (!(obj is move))
        {
            return false;
        }
        return ((move)obj).startSquare == this.startSquare && ((move)obj).endSquare == this.endSquare && ((move)obj).isSpecialMove == this.isSpecialMove && ((move)obj).specialMoveTarget == this.specialMoveTarget && ((move)obj).isPiece == this.isPiece && ((move)obj).specialMoveStart == this.specialMoveStart && ((move)obj).specialMovePiece.Equals(this.specialMovePiece);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public move(Vector2Int start, Vector2Int end)
    {
        this.startSquare = start;
        this.endSquare = end;

        this.isSpecialMove = false;
        specialMoveTarget = new Vector2Int();
        isPiece = false;
        specialMovePiece = new piece();
        specialMoveStart = new Vector2Int();
    }
}