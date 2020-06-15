using UnityEngine;
using System.Collections;

public class Drop : MonoBehaviour {

	[Header("Drop coordinates")]
	public int xIndex; // X coordinate of Drop itself
	public int yIndex; // Y coordinate of Drop itself

	Board m_board;

	bool m_isMoving = false;

	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{

	}

	public void Init(Board board)
	{
		m_board = board; 
	}

	public void SetCoordinates(int x, int y)
	{
		xIndex = x;
		yIndex = y;
	}

	public void Move (int destX, int destY, float timeToMove)
	{

		if (!m_isMoving)
		{

			StartCoroutine(MoveRoutine(new Vector3(destX, destY,0), timeToMove));	
		}
	}


	IEnumerator MoveRoutine(Vector3 destination, float timeToMove)
	{
		Vector3 startPosition = transform.position;

		bool reachedDestination = false;

		float elapsedTime = 0f;

		m_isMoving = true;

		while (!reachedDestination)
		{
			// if we are close enough to destination
			if (Vector3.Distance(transform.position, destination) < 0.01f)
			{

				reachedDestination = true;

				if (m_board !=null)
				{
					m_board.PlaceDrop(this, (int) destination.x, (int) destination.y);

				}

				break;
			}

			// track the total running time
			elapsedTime += Time.deltaTime;

			// calculate the Lerp value
			float t = Mathf.Clamp(elapsedTime / timeToMove, 0f, 1f);



			// move the drop
			transform.position = Vector3.Lerp(startPosition, destination, t);

			// wait until next frame
			yield return null;
		}

		m_isMoving = false;


	}

}
