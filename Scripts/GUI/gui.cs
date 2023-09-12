using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class gui : MonoBehaviour
{

    public GameObject tile;
    public Material lightColor;
    public Material darkColor;
    public Material lightSelected;
    public Material darkSelected;
    public Material highlightedDark;
    public Material highlightedLight;

    public GameObject kw;
    public GameObject kb;
    public GameObject qw;
    public GameObject qb;
    public GameObject rw;
    public GameObject rb;
    public GameObject bw;
    public GameObject bb;
    public GameObject nw;
    public GameObject nb;
    public GameObject pw;
    public GameObject pb;

    public GameObject canvas;

    public float scale;

    List<GameObject> images = new List<GameObject>();

    move moveInput;
    bool startGiven = false;

    Hashtable squares = new Hashtable();
    List<GameObject> changesSquares = new List<GameObject>();
    List<GameObject> highlightedSquares = new List<GameObject>();

    public TMP_Text text;
    bool gameRunning = true;

    // audio from: https://movie-sounds.org/sci-fi-movie-samples/quotes-with-sound-clips-from-2001-a-space-odyssey-1968/thank-you-for-a-very-enjoyable-game-yeah-thank-you
    public UnityEngine.Video.VideoPlayer videoPlayer;
    // audio from: https://pixabay.com/de/sound-effects/chess-pieces-60890/
    AudioSource source;

    public const int searchTime = 3;
    public bool engineIsWhite = false;

    public void Start()
    {
        // firstly creating the representation
        CreateBoard();

        source = GetComponent<AudioSource>();
        videoPlayer.Prepare();
        videoPlayer.enabled = true;

        SetEngineBlack();
        NewGame();

        evaluation.Initialize();
    }

    public void NewGame()
    {
        // setting everything back for a new game
        board.SetStart();
        board.game.legalMoves = move_generator.GenerateLegalMoves(board.game);

        ShowPosition(board.game);
        
        text.text = "Game running";
        gameRunning = true;
        moveInput = new move();
        startGiven = false;
        foreach (GameObject o in changesSquares)
        {
            Vector2Int position = o.GetComponent<identifier>().index;
            if ((position.x + position.y) % 2 != 0)
            {
                o.GetComponent<SpriteRenderer>().material = darkColor;
            }
            else
            {
                o.GetComponent<SpriteRenderer>().material = lightColor;
            }
        }
        changesSquares.Clear();
        foreach (GameObject o in highlightedSquares)
        {
            Vector2Int position = o.GetComponent<identifier>().index;
            if ((position.x + position.y) % 2 != 0)
            {
                o.GetComponent<SpriteRenderer>().material = darkColor;
            }
            else
            {
                o.GetComponent<SpriteRenderer>().material = lightColor;
            }
        }
        highlightedSquares.Clear();

        evaluation.Clear();
    }
    
    public void Update()
    {
        if (gameRunning)
        {
            // if it is the engines turn
            if (board.game.whitesTurn == engineIsWhite)
            {
                // we call the evaluation class and play the move
                move response = evaluation.GetBestMove(searchTime);
                board.MakeMove(response);

                logger.LogPosition(board.game);

                // playing the piece sound
                source.Play();

                ShowPosition(board.game);
                HighlightMove(response);

                // checking for draws and checkmates
                board.game.legalMoves = move_generator.GenerateLegalMoves(board.game);
                if (board.game.legalMoves.Count == 0)
                {
                    if (board_helper.IsInCheck((board.game.whitesTurn ? board.game.whiteKingSquare : board.game.blackKingSquare), board.game, board.game.whitesTurn))
                    {
                        text.text = "Computer won!";

                        // Playing a video. You'll see!
                        videoPlayer.enabled = true;
                        videoPlayer.Play();
                        videoPlayer.loopPointReached += EndReached;
                    }
                    else
                    {
                        text.text = "Draw";
                    }

                    gameRunning = false;
                }
                // checking the repetition table for draws
                if (board.RepetitionTableContains(zobrist_hasher.positionHash))
                {
                    ulong[] temp = board.repetitionTable.ToArray();
                    int repetitions = temp.Count(hash => hash == zobrist_hasher.positionHash);
                    if (repetitions >= 3)
                    {
                        text.text = "Draw";
                        gameRunning = false;
                    }
                }
                if (board_helper.GetNumPieces(board.game) == 2)
                {
                    text.text = "Draw";
                    gameRunning = false;
                }
                board.AddToRepetitionTable(zobrist_hasher.positionHash);
            }
            else
            {
                // waiting for user input and then playing the move if it is legal
                if (GetUserInput())
                {
                    int index = board_helper.GetLegal(moveInput, board.game.legalMoves);
                    if (index >= 0)
                    {
                        move legalMove = board.game.legalMoves[index];
                        board.MakeMove(legalMove);

                        logger.LogPosition(board.game);

                        source.Play();

                        ShowPosition(board.game);
                        HighlightMove(legalMove);

                        board.game.legalMoves = move_generator.GenerateLegalMoves(board.game);

                        if (board.game.legalMoves.Count == 0)
                        {
                            if (board_helper.IsInCheck((board.game.whitesTurn ? board.game.whiteKingSquare : board.game.blackKingSquare), board.game, board.game.whitesTurn))
                            {
                                text.text = "Player won";
                            }
                            else
                            {
                                text.text = "Draw";
                            }

                            gameRunning = false;
                        }
                        if (board.RepetitionTableContains(zobrist_hasher.positionHash))
                        {
                            ulong[] temp = board.repetitionTable.ToArray();
                            int repetitions = temp.Count(hash => hash == zobrist_hasher.positionHash);
                            if (repetitions >= 3)
                            {
                                text.text = "Draw";
                                gameRunning = false;
                            }
                        }
                        if (board_helper.GetNumPieces(board.game) == 2)
                        {
                            text.text = "Draw";
                            gameRunning = false;
                        }

                        board.AddToRepetitionTable(zobrist_hasher.positionHash);
                    }
                }
            }
        }
    }
    
    // setting the color of our engine
    public void SetEngineBlack()
    {
        engineIsWhite = false;
    }

    public void SetEngineWhite()
    {
        engineIsWhite = true;
    }

    void EndReached(UnityEngine.Video.VideoPlayer vp)
    {
        vp.enabled = false;
    }

    // qutting the programm if it is properly executed
    public void QuitGame()
    {
        Application.Quit();
    }
    
    bool GetUserInput()
    {
        // checking if the mouse buttin us pressed
        // and getting the stored index of the tile
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

            if (hit.collider != null)
            {
                Vector2Int index = hit.collider.gameObject.GetComponent<identifier>().index;

                if (startGiven)
                {
                    moveInput.endSquare = index;
                    startGiven = false;

                    ShowSelectedSquares(moveInput, board.game.legalMoves);

                    if (moveInput.startSquare != moveInput.endSquare)
                    {
                        return true;
                    }
                }
                else
                {
                    moveInput.startSquare = index;
                    startGiven = true;

                    ShowSelectedSquares(moveInput, board.game.legalMoves);
                }
            }
        }

        return false;
    }

    void HighlightMove(move move)
    {
        // highlighting moves on the board for a better user experience
        foreach (GameObject o in highlightedSquares)
        {
            Vector2Int position = o.GetComponent<identifier>().index;
            if ((position.x + position.y) % 2 != 0)
            {
                o.GetComponent<SpriteRenderer>().material = darkColor;
            }
            else
            {
                o.GetComponent<SpriteRenderer>().material = lightColor;
            }
        }
        highlightedSquares.Clear();

        GameObject start = (GameObject)squares[move.startSquare];
        highlightedSquares.Add(start);

        Vector2Int p = start.GetComponent<identifier>().index;
        if ((p.x + p.y) % 2 != 0)
        {
            start.GetComponent<SpriteRenderer>().material = highlightedDark;
        }
        else
        {
            start.GetComponent<SpriteRenderer>().material = highlightedLight;
        }

        GameObject end = (GameObject)squares[move.endSquare];
        highlightedSquares.Add(end);

        p = end.GetComponent<identifier>().index;
        if ((p.x + p.y) % 2 != 0)
        {
            end.GetComponent<SpriteRenderer>().material = highlightedDark;
        }
        else
        {
            end.GetComponent<SpriteRenderer>().material = highlightedLight;
        }
    }

    void ShowSelectedSquares(move input, List<move> legal)
    {
        // showing the user every square they can move to with the piece they selected
        foreach (GameObject o in changesSquares)
        {
            Vector2Int position = o.GetComponent<identifier>().index;
            if ((position.x + position.y) % 2 != 0)
            {
                o.GetComponent<SpriteRenderer>().material = darkColor;
            }
            else
            {
                o.GetComponent<SpriteRenderer>().material = lightColor;
            }
        }
        changesSquares.Clear();

        if (startGiven)
        {
            GameObject o = (GameObject)squares[input.startSquare];

            Vector2Int position = o.GetComponent<identifier>().index;
            if ((position.x + position.y) % 2 != 0)
            {
                o.GetComponent<SpriteRenderer>().material = darkSelected;
            }
            else
            {
                o.GetComponent<SpriteRenderer>().material = lightSelected;
            }

            changesSquares.Add(o);

            for (int i = 0; i < legal.Count; i++)
            {
                if (legal[i].startSquare == input.startSquare)
                {
                    o = (GameObject)squares[legal[i].endSquare];

                    position = o.GetComponent<identifier>().index;
                    if ((position.x + position.y) % 2 != 0)
                    {
                        o.GetComponent<SpriteRenderer>().material = darkSelected;
                    }
                    else
                    {
                        o.GetComponent<SpriteRenderer>().material = lightSelected;
                    }

                    changesSquares.Add(o);
                }
            }
        }
    }

    public void CreateBoard()
    {
        // creating the board
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                CreateTile(i, j);
            }
        }
    }

    void CreateTile(int i, int j)
    {
        // creating a tile using the i and j variables for y and x
        Vector3 position = new Vector3((j - 3.5f), (3.5f - i), 1);

        GameObject newObject = Instantiate(tile, position, Quaternion.identity);

        if ((i + j) % 2 != 0)
        {
            newObject.GetComponent<SpriteRenderer>().material = darkColor;
        }
        else
        {
            newObject.GetComponent<SpriteRenderer>().material = lightColor;
        }

        newObject.GetComponent<identifier>().index = new Vector2Int(j, i);

        squares[new Vector2Int(j, i)] = newObject;
    }

    public void ShowPosition(gameState game)
    {
        // first removing every piece, that is currently shown
        
        foreach (GameObject o in images.ToArray())
        {
            Destroy(o);
        }
        images.Clear();

        // and then creating pieces to show the current position
        for (int i = 0; i < game.pieces.GetLength(0); i++)
        {
            for (int j = 0; j < game.pieces.GetLength(1); j++)
            {
                piece temp = game.pieces[j, i];
                if (temp.type != board.nothing)
                {
                    GameObject image = GetImage(temp);
                    ShowPiece(image, new Vector2Int(j, i));
                }
            }
        }
    }
    
    GameObject GetImage(piece piece)
    {
        // getting the corresponding image to the stored piece
        switch (piece.type)
        {
            case board.king:
                return piece.isWhite ? kw : kb;
            case board.queen:
                return piece.isWhite ? qw : qb;
            case board.rook:
                return piece.isWhite ? rw : rb;
            case board.bishop:
                return piece.isWhite ? bw : bb;
            case board.knight:
                return piece.isWhite ? nw : nb;
            case board.pawn:
                return piece.isWhite ? pw : pb;
            default:
                return null;
        }
    }

    void ShowPiece(GameObject image, Vector2Int index)
    {
        // creating the image of the piece at the coordinates
        Vector3 pos = new Vector3((index.x - 3.5f), (3.5f - index.y), 0);

        GameObject temp = Instantiate(image, canvas.transform, false);
        temp.transform.Translate(pos);
        temp.transform.localScale = new Vector3(scale, scale, scale);

        images.Add(temp);
    }
}