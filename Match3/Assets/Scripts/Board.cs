using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Board : MonoBehaviour
{

    public int width; // Width of the board
    public int height;// Height of the board


    public int borderSize;

    public GameObject tilePrefab;
    public GameObject[] dropPrefabs;

    public float swapTime = 0.5f;

    Tile[,] m_allTiles;
    Drop[,] m_allDrops;

    Tile m_clickedTile; // List of instantiated tiles with their coordinates
    Tile m_targetTile;// List of instantiated drops with their coordinates

    void Start()
    {
        m_allTiles = new Tile[width, height]; // initialize allTiles.
        m_allDrops = new Drop[width, height]; // initialize allDrops.

        SetupTiles();
        FillBoard();

    }

    // Instantiates all tiles.
    void SetupTiles()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                GameObject tile = Instantiate(tilePrefab, new Vector3(i, j, 0), Quaternion.identity) as GameObject;

                tile.name = "Tile (" + i + "," + j + ")"; // Rename tile

                m_allTiles[i, j] = tile.GetComponent<Tile>();

                tile.transform.parent = transform;  // Locate tile in the hieararchy

                m_allTiles[i, j].Init(i, j, this); // Sets the position and board.

            }
        }
    }

    // Picks a random drop prefab drop dropPrefabs list.	
    GameObject GetRandomDrop()
    {
        int randomIdx = Random.Range(0, dropPrefabs.Length);

        if (dropPrefabs[randomIdx] == null)
        {
            Debug.LogWarning("BOARD:  " + randomIdx + "does not contain a valid Drop prefab!"); // Runs if there is no avaliable drop in the list.
        }

        return dropPrefabs[randomIdx];
    }

    // Places drops according to FillRandom() iterations.
    public void PlaceDrop(Drop drop, int x, int y)
    {
        if (drop == null)
        {
            Debug.LogWarning("BOARD:  Invalid Drop!");
            return;
        }

        drop.transform.position = new Vector3(x, y, 0); // Sets the position of the drop accorting to FillRandom iteration.
        drop.transform.rotation = Quaternion.identity; //Sets the rotation of the drop as default.

        if (IsWithinBounds(x, y))
        {
            m_allDrops[x, y] = drop;
        }

        drop.SetCoordinates(x, y); // Initialize coordinates of the given drop.
    }

    // Checks whether given coortinates inside the board or not.
    bool IsWithinBounds(int x, int y)
    {
        return (x >= 0 && x < width && y >= 0 && y < height);
    }

    // Instantiates drop at specific location.
    Drop FillRandomAt(int x, int y)
    {
        GameObject randomdrop = Instantiate(GetRandomDrop(), Vector3.zero, Quaternion.identity) as GameObject; // Instantition as Gameobject

        if (randomdrop != null)
        {
            randomdrop.GetComponent<Drop>().Init(this); // Initialize the board for given drop.
            PlaceDrop(randomdrop.GetComponent<Drop>(), x, y);
            randomdrop.transform.parent = transform; // Hierarchy tidying.
            return randomdrop.GetComponent<Drop>();
        }
        return null;
    }

    // Iterates every drop positions
    void FillBoard()
    {
        int maxInterations = 100;
        int iterations = 0;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Drop drop = FillRandomAt(i, j);

                while (HasMatchOnFill(i, j)) // If there is a match when filling the board.
                {
                    CleardropAt(i, j); // Clear the drop which is matches.
                    drop = FillRandomAt(i, j); // Try another drop to fill.
                    iterations++;

                    if (iterations >= maxInterations) //In case of infinite loop.
                    {
                        Debug.Log("break =====================");
                        break;
                    }
                }

            }
        }
    }

    // Returns true if there is match when fillinf the board.
    bool HasMatchOnFill(int x, int y, int minLength = 3) 
    {
        // Only checks left and downward matches because we are filling the board from left to
        // right and down to up so, it is impossible to catch match at yop and right because they are empty.
        List<Drop> leftMatches = FindMatches(x, y, new Vector2(-1, 0), minLength);
        List<Drop> downwardMatches = FindMatches(x, y, new Vector2(0, -1), minLength);

        if (leftMatches == null)
        {
            leftMatches = new List<Drop>();
        }

        if (downwardMatches == null)
        {
            downwardMatches = new List<Drop>();
        }

        return (leftMatches.Count > 0 || downwardMatches.Count > 0);

    }

    // Tile that ve clicked first.
    public void ClickTile(Tile tile)
    {
        if (m_clickedTile == null)// Defensive programming
        {
            m_clickedTile = tile;
            //Debug.Log("clicked tile: " + tile.name);
        }
    }

    // Tile that ve drag to.
    public void DragToTile(Tile tile)
    {
        if (m_clickedTile != null && IsNextTo(tile, m_clickedTile)) // Defensive programming and checking is first tile and target tiles are neighbor.
        {
            m_targetTile = tile;
        }
    }

    // Tile that we relase.
    public void ReleaseTile()
    {
        if (m_clickedTile != null && m_targetTile != null)  // Defensive programming 
        {
            SwitchTiles(m_clickedTile, m_targetTile);
        }

        m_clickedTile = null; // After switch set clicked tile to null.
        m_targetTile = null; // After switch set target tile to null.
    }

    // Function to start coroutine movement between clickedTile and targetTile
    void SwitchTiles(Tile clickedTile, Tile targetTile)
    {
        StartCoroutine(SwitchTilesRoutine(clickedTile, targetTile));
    }

    // Actual switching function.
    IEnumerator SwitchTilesRoutine(Tile clickedTile, Tile targetTile)
    {
        Drop clickedDrop= m_allDrops[clickedTile.xIndex, clickedTile.yIndex];
        Drop targetDrop = m_allDrops[targetTile.xIndex, targetTile.yIndex];

        if (targetDrop != null && clickedDrop != null)
        {
            clickedDrop.Move(targetTile.xIndex, targetTile.yIndex, swapTime);  // Moves the clickedTile to position of taget tile
            targetDrop.Move(clickedTile.xIndex, clickedTile.yIndex, swapTime);  // Moves the targerTile to position of clicked tile

            yield return new WaitForSeconds(swapTime); // Waits until Drops switch movement ends.

            List<Drop> clickeddropMatches = FindMatchesAt(clickedTile.xIndex, clickedTile.yIndex); // Check for matches
            List<Drop> targetdropMatches = FindMatchesAt(targetTile.xIndex, targetTile.yIndex); // Check for matches

            if (targetdropMatches.Count == 0 && clickeddropMatches.Count == 0)  // Swich back if there is no match.
            {
                clickedDrop.Move(clickedTile.xIndex, clickedTile.yIndex, swapTime);
                targetDrop.Move(targetTile.xIndex, targetTile.yIndex, swapTime);
            }
            else
            {
                yield return new WaitForSeconds(swapTime);

                ClearBoard(clickeddropMatches.Union(targetdropMatches).ToList());

            }
        }


    }

    // Returns true if there is 1 unit (neighbor) between clickedTile and targetTile
    bool IsNextTo(Tile start, Tile end)
    {
        if (Mathf.Abs(start.xIndex - end.xIndex) == 1 && start.yIndex == end.yIndex)
        {
            return true;
        }

        if (Mathf.Abs(start.yIndex - end.yIndex) == 1 && start.xIndex == end.xIndex)
        {
            return true;
        }

        return false;
    }

    // general search method; specify a starting coordinate (startX, startY) and use a Vector2 for direction
    // i.e. (1,0) = right, (-1,0) = left, (0,1) = up, (0,-1) = down; minLength is minimum number to be considered
    // a match

    //  !!! Actual finding match function !!!
    List<Drop> FindMatches(int startX, int startY, Vector2 searchDirection, int minLength = 3)
    {
        List<Drop> matches = new List<Drop>(); // Initialize matches list that we returns at the end of the function,

        Drop startdrop = null;  // Initialization of local variable.

        if (IsWithinBounds(startX, startY))
        {
            startdrop = m_allDrops[startX, startY];  // Sets the coordinates of the drop.
        }

        if (startdrop != null)
        {
            matches.Add(startdrop);  // Adding the startDrop to matches List.
        }
        else
        {
            return null;
        }

        int nextX;
        int nextY;

        int maxValue = (width > height) ? width : height; // Returns greather for max num of iteration that we check.

        for (int i = 1; i < maxValue - 1; i++)
        {
            nextX = startX + (int)Mathf.Clamp(searchDirection.x, -1, 1) * i; // Increase or decrease 1 (according to searchDiretion) to X axis at every iteration. 
            nextY = startY + (int)Mathf.Clamp(searchDirection.y, -1, 1) * i; // Increase or decrease 1 (according to searchDiretion) to Y axis at every iteration. 

            if (!IsWithinBounds(nextX, nextY))  // Works if board is not square
            {
                break;
            }

            Drop nextdrop = m_allDrops[nextX, nextY];  // Sets nextDrop

            if (nextdrop == null) // if neighbor drop is null
            {
                break;
            }
            else  // if neighbor drop is not null
            {
                if (nextdrop.tag == startdrop.tag && !matches.Contains(nextdrop)) // Check whether nextDrop and startDrops shares the same tag or not
                {
                    matches.Add(nextdrop); // Add nextDrop to matches list.
                }
                else
                {
                    break;  // startDrop and nextDrop are not same color.
                }
            }
        }

        if (matches.Count >= minLength) // Check length of matches list is greather then minLength (in our case min length is 3),
        {
            return matches;
        }

        return null;  // No match

    }


    // Returns matches on Y axis.
    List<Drop> FindVerticalMatches(int startX, int startY, int minLength = 3)
    {
        List<Drop> upwardMatches = FindMatches(startX, startY, new Vector2(0, 1), 2);  // Sets upwardMatces to matches list that return from FindMatches().
        List<Drop> downwardMatches = FindMatches(startX, startY, new Vector2(0, -1), 2);  // Sets downwardMatces to matches list that return from FindMatches().

        if (upwardMatches == null)
        {
            upwardMatches = new List<Drop>();
        }

        if (downwardMatches == null)
        {
            downwardMatches = new List<Drop>();
        }

        var combinedMatches = upwardMatches.Union(downwardMatches).ToList(); // combines upward and downward matches (Union operation return Enumerable type so we last is as List)

        return (combinedMatches.Count >= minLength) ? combinedMatches : null; // returns combinedMatches List if legth of list is greather then minLength.

    }

    // Returns matches on Y axis.
    List<Drop> FindHorizontalMatches(int startX, int startY, int minLength = 3)
    {
        List<Drop> rightMatches = FindMatches(startX, startY, new Vector2(1, 0), 2); // Sets rightMatces to matches list that return from FindMatches().
        List<Drop> leftMatches = FindMatches(startX, startY, new Vector2(-1, 0), 2); // Sets leftMatces to matches list that return from FindMatches().

        if (rightMatches == null)
        {
            rightMatches = new List<Drop>();
        }

        if (leftMatches == null)
        {
            leftMatches = new List<Drop>();
        }

        var combinedMatches = rightMatches.Union(leftMatches).ToList(); // combines left and right mathes (Union operation return Enumerable type so we last is as List)

        return (combinedMatches.Count >= minLength) ? combinedMatches : null; // returns combinedMatches List if legth of list is greather then minLength.

    }

    // Finds matches at specific location then combine vertical and horizontal matches.
    List<Drop> FindMatchesAt(int x, int y, int minLength = 3)
    {
        List<Drop> horizMatches = FindHorizontalMatches(x, y, minLength); 
        List<Drop> vertMatches = FindVerticalMatches(x, y, minLength);

        if (horizMatches == null)
        {
            horizMatches = new List<Drop>();
        }

        if (vertMatches == null)
        {
            vertMatches = new List<Drop>();
        }
        var combinedMatches = horizMatches.Union(vertMatches).ToList(); // combines horizontal and vertical matches (Union operation return Enumerable type so we last is as List)
        return combinedMatches;
    }

    // Overloaded version to reference directly list element.
    List<Drop> FindMatchesAt(List<Drop> drops, int minLength = 3)
    {
        List<Drop> matches = new List<Drop>();

        foreach (Drop drop in drops)
        {
            matches = matches.Union(FindMatchesAt(drop.xIndex, drop.yIndex, minLength)).ToList(); // Finds the location of the parameter.
        }

        return matches;
    }

    void HighlightTileOff(int x, int y)
    {
        SpriteRenderer spriteRenderer = m_allTiles[x, y].GetComponent<SpriteRenderer>();
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, .5f);
    }

    void HighlightTileOn(int x, int y, Color col)
    {
        SpriteRenderer spriteRenderer = m_allTiles[x, y].GetComponent<SpriteRenderer>();
        spriteRenderer.color = col;
    }

    // Highlight the matches at specific location. (Just for testing)
    void HighlightMatchesAt(int x, int y)
    {
        HighlightTileOff(x, y);
        var combinedMatches = FindMatchesAt(x, y);
        if (combinedMatches.Count > 0) // If there is a combined match.
        {
            foreach (Drop drop in combinedMatches)
            {
                HighlightTileOn(drop.xIndex, drop.yIndex, drop.GetComponent<SpriteRenderer>().color); //Highlight drop at given location.
            }
        }
    }


    void Highlightdrops(List<Drop> drops)
    {
        foreach (Drop drop in drops)
        {
            if (drop != null)
            {
                HighlightTileOn(drop.xIndex, drop.yIndex, drop.GetComponent<SpriteRenderer>().color);
            }
        }
    }

    // Clears drop specific location with parameter x,y coordinates
    void CleardropAt(int x, int y)
    {
        Drop dropToClear = m_allDrops[x, y];

        if (dropToClear != null)
        {
            m_allDrops[x, y] = null;
            Destroy(dropToClear.gameObject);
        }

        HighlightTileOff(x, y);
    }


    // Clears drop specific location with parameter List<Drop>
    void CleardropAt(List<Drop> drops)
    {
        foreach (Drop drop in drops)
        {
            if (drop != null)
            {
                CleardropAt(drop.xIndex, drop.yIndex);
            }
        }
    }


    // Collapsing column with paramer of coordinate and collapse time. 
    List<Drop> CollapseColumn(int column, float collapseTime = 0.1f)
    {
        List<Drop> movingDrops = new List<Drop>();

        for (int i = 0; i < height - 1; i++) //Looks all the way up to height - 1 (because most top column can not collapse)
        {
            if (m_allDrops[column, i] == null) // If there is a empty columm
            {
                for (int j = i + 1; j < height; j++) // Looks all the way up from empty columm
                {
                    if (m_allDrops[column, j] != null) // If there is not empty column
                    {
                        m_allDrops[column, j].Move(column, i, collapseTime); // Move drop from [column,j] to [column,i] 
                        m_allDrops[column, i] = m_allDrops[column, j]; // Initialize movement
                        m_allDrops[column, i].SetCoordinates(column, i); // Set coordinates ad Drop.cs

                        if (!movingDrops.Contains(m_allDrops[column, i]))
                        {
                            movingDrops.Add(m_allDrops[column, i]);
                        }

                        m_allDrops[column, j] = null;
                        break;

                    }
                }
            }
        }
        return movingDrops; // Pass moving drop

    }

    //Collapse column with parameter of List<Drop>
    List<Drop> CollapseColumn(List<Drop> drops)
    {
        List<Drop> movingDrops = new List<Drop>();
        List<int> columnsToCollapse = GetColumns(drops);

        foreach (int column in columnsToCollapse)
        {
            movingDrops = movingDrops.Union(CollapseColumn(column)).ToList(); //Union movingDrop with new moving drop.
        }

        return movingDrops;

    }

    List<int> GetColumns(List<Drop> drops)
    {
        List<int> columns = new List<int>();

        foreach (Drop drop in drops)
        {
            if (!columns.Contains(drop.xIndex))
            {
                columns.Add(drop.xIndex);
            }
        }

        return columns;

    }
    
    void ClearBoard(List<Drop> drops)
    {
        StartCoroutine(ClearAndCollapseRoutine(drops));
    }

    // Clear and collapse drop until matches end
    IEnumerator ClearAndCollapseRoutine(List<Drop> drops)
    {
        List<Drop> movingdrops = new List<Drop>();
        List<Drop> matches = new List<Drop>();

        Highlightdrops(drops);
        yield return new WaitForSeconds(0.25f);
        bool isFinished = false;

        while (!isFinished)
        {
            CleardropAt(drops); //Clear specific drop

            yield return new WaitForSeconds(0.25f);
            movingdrops = CollapseColumn(drops); // Collapse specific column
            yield return new WaitForSeconds(0.25f);

            matches = FindMatchesAt(movingdrops); // Check is there a match

            if (matches.Count == 0) //If there is no more mathes
            {
                isFinished = true; // exit the while loop
                break;
            }
            else // If there is still matches call function itself
            {
                yield return StartCoroutine(ClearAndCollapseRoutine(matches));
            }
        }
        yield return null;
    }
}
