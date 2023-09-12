using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class board_helper
{
    // a class with functions which are used in all the different classes

    // getting a piece type by it's char (for fen transformation)
    public static piece GetPiece(char character)
    {
        piece piece = new piece();

        if (Char.IsUpper(character))
        {
            piece.isWhite = true;
        }
        else
        {
            piece.isWhite = false;
        }

        switch (Char.ToLower(character))
        {
            case 'k':
                piece.type = board.king;
                return piece;
            case 'q':
                piece.type = board.queen;
                return piece;
            case 'r':
                piece.type = board.rook;
                return piece;
            case 'b':
                piece.type = board.bishop;
                return piece;
            case 'n':
                piece.type = board.knight;
                return piece;
            case 'p':
                piece.type = board.pawn;
                return piece;
            default:
                piece.type = board.nothing;
                return piece;
        }
    }

    // checking if a piece contains to the current side to move
    public static bool IsTurnColor(piece piece, gameState game)
    {
        if (piece.type == board.nothing)
        {
            return false;
        }

        if (piece.isWhite == game.whitesTurn)
        {
            return true;
        }

        return false;
    }

    // checking if a move would bring a piece over the edges of the board
    public static bool IsOverEdge(Vector2Int index, Vector2Int move)
    {
        Vector2Int square = index + move;
        if (square.x >= 0 && square.x <= 7 && square.y >= 0 && square.y <= 7)
        {
            return false;
        }

        return true;
    }

    // searching for the legal move in a list (for converting user input)
    public static int GetLegal(move move, List<move> legalMoves)
    {
        for (int i = 0, n = legalMoves.Count; i < n; i++)
        {
            if (move.startSquare == legalMoves[i].startSquare && move.endSquare == legalMoves[i].endSquare)
            {
                return i;
            }
        }

        return -1;
    }

    // checking if a specified square is in check by reverse looking in every direction
    public static bool IsInCheck(Vector2Int index, gameState game, bool kingWhite)
    {
        Vector2Int[] rook = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
        Vector2Int[] bishop = { new Vector2Int(1, 1), new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 1) };
        Vector2Int[] knight = { new Vector2Int(2, 1), new Vector2Int(1, 2), new Vector2Int(2, -1), new Vector2Int(-1, 2), new Vector2Int(-2, 1), new Vector2Int(-2, -1), new Vector2Int(-1, -2), new Vector2Int(1, -2) };
        Vector2Int[] king = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 1), new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 1) };
        Vector2Int[] black = { new Vector2Int(1, 1), new Vector2Int(-1, 1) };
        Vector2Int[] white = { new Vector2Int(1, -1), new Vector2Int(-1, -1) };

        for (int i = 0, n = rook.Length; i < n; i++)
        {
            for (int j = 1; !IsOverEdge(index, rook[i] * j); j++)
            {
                Vector2Int position = index + rook[i] * j;
                if (game.pieces[position.x, position.y].type != board.nothing)
                {
                    if (game.pieces[position.x, position.y].isWhite != kingWhite && (game.pieces[position.x, position.y].type == board.rook || game.pieces[position.x, position.y].type == board.queen))
                    {
                        return true;
                    }
                    break;
                }
            }
        }

        for (int i = 0, n = bishop.Length; i < n; i++)
        {
            for (int j = 1; !IsOverEdge(index, bishop[i] * j); j++)
            {
                Vector2Int position = index + bishop[i] * j;
                if (game.pieces[position.x, position.y].type != board.nothing)
                {
                    if (game.pieces[position.x, position.y].isWhite != kingWhite && (game.pieces[position.x, position.y].type == board.bishop || game.pieces[position.x, position.y].type == board.queen))
                    {
                        return true;
                    }
                    break;
                }
            }
        }

        for (int i = 0, n = knight.Length; i < n; i++)
        {
            if (!IsOverEdge(index, knight[i]))
            {
                Vector2Int position = index + knight[i];
                if (game.pieces[position.x, position.y].isWhite != kingWhite && game.pieces[position.x, position.y].type == board.knight)
                {
                    return true;
                }
            }
        }

        for (int i = 0, n = king.Length; i < n; i++)
        {
            if (!IsOverEdge(index, king[i]))
            {
                Vector2Int position = index + king[i];
                if (game.pieces[position.x, position.y].isWhite != kingWhite && game.pieces[position.x, position.y].type == board.king)
                {
                    return true;
                }
            }
        }

        if (kingWhite)
        {
            for (int i = 0, n = white.Length; i < n; i++)
            {
                if (!IsOverEdge(index, white[i]))
                {
                    Vector2Int position = index + white[i];
                    if (game.pieces[position.x, position.y].isWhite != kingWhite && game.pieces[position.x, position.y].type == board.pawn)
                    {
                        return true;
                    }
                }
            }
        }
        else
        {
            for (int i = 0, n = black.Length; i < n; i++)
            {
                if (!IsOverEdge(index, black[i]))
                {
                    Vector2Int position = index + black[i];
                    if (game.pieces[position.x, position.y].isWhite != kingWhite && game.pieces[position.x, position.y].type == board.pawn)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // transforming move notation to a processable square
    public static Vector2Int TransformNotation(string notation)
    {
        int x = TransformLetter(notation[0]);
        int y = int.Parse(notation[1].ToString()) - 1;

        return new Vector2Int(x, y);
    }

    // transforming the "letter-part" of the notation
    public static int TransformLetter(char letter)
    {
        switch (letter)
        {
            case 'a':
                return 0;
            case 'b':
                return 1;
            case 'c':
                return 2;
            case 'd':
                return 3;
            case 'e':
                return 4;
            case 'f':
                return 5;
            case 'g':
                return 6;
            case 'h':
                return 7;
            default:
                return -1;
        }
    }

    // getting the whole number of pieces
    public static int GetNumPieces(gameState game)
    {
        int count = game.whiteSquares.Count + game.blackSquares.Count;

        return count;
    }

    // counting non paws and kings (used for determining the current phase of the game)
    public static int CountNonPawns(gameState game)
    {
        int count = 0;
        
        for (int i = 0, n = game.whiteSquares.Count; i < n; i++)
        {
            Vector2Int index = game.whiteSquares[i];

            if (game.pieces[index.x, index.y].type != board.pawn && game.pieces[index.x, index.y].type != board.king)
            {
                count++;
            }
        }
        for (int i = 0, n = game.blackSquares.Count; i < n; i++)
        {
            Vector2Int index = game.blackSquares[i];

            if (game.pieces[index.x, index.y].type != board.pawn && game.pieces[index.x, index.y].type != board.king)
            {
                count++;
            }
        }

        return count;
    }

    // getting the squares of the pieces of one color
    // (for initializing the square lists after transforming the start fen)
    public static List<Vector2Int> GetPieceSquares(gameState game, bool isWhite)
    {
        List<Vector2Int> pieceSquares = new List<Vector2Int>();
        for (int i = 0, n = game.pieces.GetLength(0); i < n; i++)
        {
            for (int j = 0, o = game.pieces.GetLength(1); j < o; j++)
            {
                if (game.pieces[j, i].type != board.nothing && game.pieces[j, i].isWhite == isWhite)
                {
                    pieceSquares.Add(new Vector2Int(j, i));
                }
            }
        }

        return pieceSquares;
    }

    // getting the king index from a list of squares
    // (for initializing of the king square)
    public static Vector2Int GetKingIndex(gameState game, List<Vector2Int> pieceSquares)
    {
        for (int i = 0, n = pieceSquares.Count; i < n; i++)
        {
            if (game.pieces[pieceSquares[i].x, pieceSquares[i].y].type == board.king)
            {
                return pieceSquares[i];
            }
        }

        return new Vector2Int(-1, -1);
    }

    // getting the index into an array using a two dimensional index
    public static int GetTableIndex(Vector2Int index, bool isWhite)
    {
        if (!isWhite)
        {
            index.x = 7 - index.x;
            index.y = 7 - index.y;
        }
        int i = (index.y * 8) + index.x;

        return i;
    }
}
