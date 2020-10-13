using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.UI
{
    public class UITest
    {

        protected GameManager GameManager;

        [UnitySetUp]
        public IEnumerator UnitySetup()
        {
            Debug.LogFormat("UnitySetup");
            yield return SceneManager.LoadSceneAsync("FlameSymbol", LoadSceneMode.Single);
            //SceneManager.LoadScene("FlameSymbol", LoadSceneMode.Single);
            //yield return null;
        }

        [SetUp]
        public void Setup()
        {
            Debug.LogFormat("Setup");
            GameManager = Object.FindObjectOfType<GameManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(GameManager.gameObject);
        }

        public IEnumerator DownArrow()
        {
            GameManager.Cursor.OnArrow(0, -1);
            yield return null;
        }

        public IEnumerator UpArrow()
        {
            GameManager.Cursor.OnArrow(0, 1);
            yield return null;
        }

        public IEnumerator RightArrow()
        {
            GameManager.Cursor.OnArrow(1, 0);
            yield return null;
        }

        public IEnumerator LeftArrow()
        {
            GameManager.Cursor.OnArrow(-1, 0);
            yield return null;
        }

        public IEnumerator Enter()
        {
            GameManager.Cursor.OnSubmit();
            yield return null;
        }

        public IEnumerator Cancel()
        {
            GameManager.Cursor.OnCancel();
            yield return null;
        }

        /// <summary>
        /// Helper method to move the cursor to the desired position by pressing the arrow keys.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public IEnumerator MoveCursor(float x, float y)
        {
            Cursor cursor = GameManager.Cursor;
            Vector2 currentPosition = cursor.transform.position;

            float xDifference = x - currentPosition.x;
            float yDifference = y - currentPosition.y;

            float xSign = Mathf.Sign(xDifference);
            float ySign = Mathf.Sign(yDifference);

            float xDistance = Mathf.Abs(xDifference);
            float yDistance = Mathf.Abs(yDifference);

            for (int i = 0; i < xDistance; i++)
            {
                cursor.OnArrow(xSign, 0);
            }

            for (int i = 0; i < yDistance; i++)
            {
                cursor.OnArrow(0, ySign);
            }

            yield return null;
        }
    }
}
