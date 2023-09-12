using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class move_generator
{
    static bool generateQuietMoves;
    // generating legal moves by first getting pseudo legal moves
    // and filtering the illegal moves
    public static List<move> GenerateLegalMoves(gameState game, bool generateNonCaptures = true)
    {
        List<move> moves = GeneratePseudoLegalMoves(game, generateNonCaptures);

        return FilterIllegalMoves(moves, game);
    }

    // generating pseudo legal moves by looping over every square with a piece
    // and generating the moves for it
    public static List<move> GeneratePseudoLegalMoves(gameState game, bool generateNonCaptures = true)
    {
        generateQuietMoves = generateNonCaptures;
        List<move> moves = new List<move>();

        for (int i = 0, n = (game.whitesTurn ? game.whiteSquares.Count : game.blackSquares.Count); i < n; i++)
        {
            Vector2Int index = (game.whitesTurn ? game.whiteSquares[i] : game.blackSquares[i]);
            moves.AddRange(GeneratePieceMoves(game, game.pieces[index.x, index.y], index));
        }

        return moves;
    }

    // filtering illegal moves by playing every one and
    // checking if they leave our king in check
    static List<move> FilterIllegalMoves(List<move> pseudoLegalMoves, gameState position)
    {
        List<move> legalMoves = new List<move>();
        gameState storedGameState = new gameState(board.game);
        ulong storedHash = zobrist_hasher.positionHash;

        board.game = new gameState(position);
        foreach (move m in pseudoLegalMoves)
        {
            board.MakeMove(m);
            
            if (!board_helper.IsInCheck((position.whitesTurn ? board.game.whiteKingSquare : board.game.blackKingSquare), board.game, position.whitesTurn))
            {
                legalMoves.Add(m);
            }

            board.UnmakeMove(new gameState(position), storedHash);
        }

        board.game = new gameState(storedGameState);

        return legalMoves;
    }

    // generating moves for a piece by seperating it into sliding pieces etc.
    static List<move> GeneratePieceMoves(gameState game, piece piece, Vector2Int index)
    {
        if (piece.type == board.queen || piece.type == board.rook || piece.type == board.bishop)
        {
            return GenerateSlidingPieceMoves(game, piece, index);
        }
        else if (piece.type == board.pawn)
        {
            return GeneratePawnMoves(game, piece, index);
        }
        else
        {
            return GenerateOtherPieceMoves(game, piece, index);
        }
    }

    // generating sliding piece moves by looping over every direction to go into
    // until you hit the edge or another piece and adding moves along the way
    static List<move> GenerateSlidingPieceMoves(gameState game, piece piece, Vector2Int index)
    {
        List<Vector2Int> directions = new List<Vector2Int>();

        Vector2Int[] queen = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 1), new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 1) };
        Vector2Int[] rook = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
        Vector2Int[] bishop = { new Vector2Int(1, 1), new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 1) };

        if (piece.type == board.queen)
        {
            directions.AddRange(queen);
        }
        else if (piece.type == board.rook)
        {
            directions.AddRange(rook);
        }
        else if (piece.type == board.bishop)
        {
            directions.AddRange(bishop);
        }

        List<move> pieceMoves = new List<move>();

        for (int i = 0, n = directions.Count; i < n; i++)
        {
            for (int j = 1; !board_helper.IsOverEdge(index, directions[i] * j); j++)
            {
                move newMove = new move();
                newMove.startSquare = index;
                newMove.endSquare = index + (directions[i] * j);
                newMove.isSpecialMove = false;

                if (board_helper.IsTurnColor(game.pieces[newMove.endSquare.x, newMove.endSquare.y], game))
                {
                    break;
                }
                if (generateQuietMoves)
                {
                    pieceMoves.Add(newMove);
                }

                if (game.pieces[newMove.endSquare.x, newMove.endSquare.y].type != board.nothing)
                {
                    if (!generateQuietMoves)
                    {
                        pieceMoves.Add(newMove);
                    }

                    break;
                }
            }
        }

        return pieceMoves;
    }

    // generating pawn moves by setting the directions depending on the color
    // and checking if there is a piece or not, as needed
    static List<move> GeneratePawnMoves(gameState game, piece piece, Vector2Int index)
    {
        List<move> pieceMoves = new List<move>();

        Vector2Int doubleMove;
        int startHeight;
        Vector2Int normalMove;
        List<Vector2Int> captures = new List<Vector2Int>();
        int endHeight;
        List<piece> promotionPieces = new List<piece>();

        Vector2Int[] whiteCaptures = { new Vector2Int(1, -1), new Vector2Int(-1, -1) };
        Vector2Int[] blackCaptures = { new Vector2Int(1, 1), new Vector2Int(-1, 1) };

        piece[] whitePromotions = { new piece(board.queen, true), new piece(board.rook, true), new piece(board.bishop, true), new piece(board.knight, true) };
        piece[] blackPromotions = { new piece(board.queen, false), new piece(board.rook, false), new piece(board.bishop, false), new piece(board.knight, false) };

        if (piece.isWhite)
        {
            doubleMove = new Vector2Int(0, -2);
            startHeight = 6;
            normalMove = new Vector2Int(0, -1);
            captures.AddRange(whiteCaptures);
            endHeight = 0;
            promotionPieces.AddRange(whitePromotions);
        }
        else
        {
            doubleMove = new Vector2Int(0, 2);
            startHeight = 1;
            normalMove = new Vector2Int(0, 1);
            captures.AddRange(blackCaptures);
            endHeight = 7;
            promotionPieces.AddRange(blackPromotions);
        }

        move newMove = new move();
        newMove.startSquare = index;
        newMove.isSpecialMove = false;

        if (generateQuietMoves)
        {
            newMove.endSquare = index + doubleMove;
            if (index.y == startHeight && game.pieces[newMove.endSquare.x, newMove.endSquare.y].type == board.nothing && game.pieces[newMove.startSquare.x + normalMove.x, newMove.startSquare.y + normalMove.y].type == board.nothing)
            {
                pieceMoves.Add(newMove);
            }

            newMove.endSquare = index + normalMove;
            if (!board_helper.IsOverEdge(index, normalMove) && game.pieces[newMove.endSquare.x, newMove.endSquare.y].type == board.nothing)
            {
                if (newMove.endSquare.y == endHeight)
                {
                    // adding every promotion type
                    foreach (piece p in promotionPieces)
                    {
                        newMove.isSpecialMove = true;
                        newMove.specialMoveTarget = newMove.endSquare;
                        newMove.isPiece = true;
                        newMove.specialMovePiece = p;
                        pieceMoves.Add(newMove);
                    }
                }
                else
                {
                    pieceMoves.Add(newMove);
                }

                newMove.isSpecialMove = false;
            }
        }


        for (int i = 0, n = captures.Count; i < n; i++)
        {
            newMove.endSquare = index + captures[i];
            if (!board_helper.IsOverEdge(index, captures[i]) && game.pieces[newMove.endSquare.x, newMove.endSquare.y].type != board.nothing && !board_helper.IsTurnColor(game.pieces[newMove.endSquare.x, newMove.endSquare.y], game))
            {
                if (newMove.endSquare.y == endHeight)
                {
                    // adding every promotion type
                    foreach (piece p in promotionPieces)
                    {
                        newMove.isSpecialMove = true;
                        newMove.specialMoveTarget = newMove.endSquare;
                        newMove.isPiece = true;
                        newMove.specialMovePiece = p;
                        pieceMoves.Add(newMove);
                    }
                }
                else
                {
                    pieceMoves.Add(newMove);
                }

                newMove.isSpecialMove = false;
            }
            else if (game.enPassantSquares.Count != 0 && game.enPassantSquares.Contains(newMove.endSquare))
            {
                // adding a en passant square if we are in the right position relative to it
                newMove.isSpecialMove = true;
                newMove.specialMoveTarget = new Vector2Int(newMove.endSquare.x, newMove.startSquare.y);
                newMove.isPiece = true;
                newMove.specialMovePiece.type = board.nothing;

                pieceMoves.Add(newMove);

                newMove.isSpecialMove = false;
            }
        }

        return pieceMoves;
    }

    // generating king and knight moves by looping over every direction to move to
    // and adding the move if we wouldn't capture our own pieces
    static List<move> GenerateOtherPieceMoves(gameState game, piece piece, Vector2Int index)
    {
        List<Vector2Int> directions = new List<Vector2Int>();
        List<move> pieceMoves = new List<move>();

        Vector2Int[] king = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 1), new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 1) };
        Vector2Int[] knight = { new Vector2Int(2, 1), new Vector2Int(1, 2), new Vector2Int(2, -1), new Vector2Int(-1, 2), new Vector2Int(-2, 1), new Vector2Int(-2, -1), new Vector2Int(-1, -2), new Vector2Int(1, -2) };

        if (piece.type == board.king)
        {
            directions.AddRange(king);

            if (generateQuietMoves)
            {
                if (game.whitesTurn)
                {
                    // checking for hwites castling rights
                    if (game.whiteKingsite)
                    {
                        if (game.pieces[5, 7].type == board.nothing && game.pieces[6, 7].type == board.nothing)
                        {
                            if (!board_helper.IsInCheck(new Vector2Int(4, 7), game, game.whitesTurn) && !board_helper.IsInCheck(new Vector2Int(5, 7), game, game.whitesTurn) && !board_helper.IsInCheck(new Vector2Int(6, 7), game, game.whitesTurn))
                            {
                                move newMove = new move();
                                newMove.startSquare = index;
                                newMove.endSquare = new Vector2Int(6, 7);
                                newMove.isSpecialMove = true;
                                newMove.specialMoveTarget = new Vector2Int(5, 7);
                                newMove.isPiece = false;
                                newMove.specialMoveStart = new Vector2Int(7, 7);

                                pieceMoves.Add(newMove);
                            }
                        }
                    }
                    if (game.whiteQueensite)
                    {
                        if (game.pieces[1, 7].type == board.nothing && game.pieces[2, 7].type == board.nothing && game.pieces[3, 7].type == board.nothing)
                        {
                            if (!board_helper.IsInCheck(new Vector2Int(4, 7), game, game.whitesTurn) && !board_helper.IsInCheck(new Vector2Int(3, 7), game, game.whitesTurn) && !board_helper.IsInCheck(new Vector2Int(2, 7), game, game.whitesTurn))
                            {
                                move newMove = new move();
                                newMove.startSquare = index;
                                newMove.endSquare = new Vector2Int(2, 7);
                                newMove.isSpecialMove = true;
                                newMove.specialMoveTarget = new Vector2Int(3, 7);
                                newMove.isPiece = false;
                                newMove.specialMoveStart = new Vector2Int(0, 7);

                                pieceMoves.Add(newMove);
                            }
                        }
                    }
                }
                else
                {
                    // and for black castling rights
                    if (game.blackKingsite)
                    {
                        if (game.pieces[5, 0].type == board.nothing && game.pieces[6, 0].type == board.nothing)
                        {
                            if (!board_helper.IsInCheck(new Vector2Int(4, 0), game, game.whitesTurn) && !board_helper.IsInCheck(new Vector2Int(5, 0), game, game.whitesTurn) && !board_helper.IsInCheck(new Vector2Int(6, 0), game, game.whitesTurn))
                            {
                                move newMove = new move();
                                newMove.startSquare = index;
                                newMove.endSquare = new Vector2Int(6, 0);
                                newMove.isSpecialMove = true;
                                newMove.specialMoveTarget = new Vector2Int(5, 0);
                                newMove.isPiece = false;
                                newMove.specialMoveStart = new Vector2Int(7, 0);

                                pieceMoves.Add(newMove);
                            }
                        }
                    }
                    if (game.blackQueensite)
                    {
                        if (game.pieces[1, 0].type == board.nothing && game.pieces[2, 0].type == board.nothing && game.pieces[3, 0].type == board.nothing)
                        {
                            if (!board_helper.IsInCheck(new Vector2Int(4, 0), game, game.whitesTurn) && !board_helper.IsInCheck(new Vector2Int(3, 0), game, game.whitesTurn) && !board_helper.IsInCheck(new Vector2Int(2, 0), game, game.whitesTurn))
                            {
                                move newMove = new move();
                                newMove.startSquare = index;
                                newMove.endSquare = new Vector2Int(2, 0);
                                newMove.isSpecialMove = true;
                                newMove.specialMoveTarget = new Vector2Int(3, 0);
                                newMove.isPiece = false;
                                newMove.specialMoveStart = new Vector2Int(0, 0);

                                pieceMoves.Add(newMove);
                            }
                        }
                    }
                }
            }
        }
        else if (piece.type == board.knight)
        {
            directions.AddRange(knight);
        }

        for (int i = 0, n = directions.Count; i < n; i++)
        {
            if (!board_helper.IsOverEdge(index, directions[i]))
            {
                move newMove = new move();
                newMove.startSquare = index;
                newMove.endSquare = index + directions[i];
                newMove.isSpecialMove = false;

                if (board_helper.IsTurnColor(game.pieces[newMove.endSquare.x, newMove.endSquare.y], game))
                {
                    continue;
                }

                if (generateQuietMoves)
                {
                    pieceMoves.Add(newMove);
                }
                else if (game.pieces[newMove.endSquare.x, newMove.endSquare.y].type != board.nothing)
                {
                    pieceMoves.Add(newMove);
                }
            }
        }

        return pieceMoves;
    }
}
