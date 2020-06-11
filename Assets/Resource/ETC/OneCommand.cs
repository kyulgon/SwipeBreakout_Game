using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OneCommand : MonoBehaviour
{
    #region 태그에 따른 함수 호출
    private void Awake()
    {
        if (CompareTag("GameManager"))
            Awake_GM();
    }

    private void Update()
    {
        if (CompareTag("GameManager"))
            Update_GM();
    }

    private void FixedUpdate()
    {
        if (CompareTag("GameManager"))
            FixedUpdate_GM();
    }

    private void Start()
    {
        if (CompareTag("Ball"))
            Start_BALL();
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (CompareTag("Ball"))
            StartCoroutine(OnCollisionEnter2D_BALL(col));
    }



    #endregion

    #region GameManager.Cs

    [Header("GameManagerValue")]
    public float groundY = -55.489f; // Ball의 기본 Y좌표를 -55.489로 설정(바닥에 닿는 점)
    public GameObject P_Ball, P_GreenOrb, P_Block, P_ParticleBlue, P_ParticleGreen, P_ParticleRed; // 프리팹 P
    public GameObject BallPreview, Arrow, GameOverPanel, BallCountTextObj, BallPlusTextObj;
    public Transform GreenBallGroup, BlockGroup, BallGroup;
    public LineRenderer MouseLR, BallLR;
    public Text BestScoreText, ScoreText, BallCountText, BallPlusText, FinalScoreText, NewRecordText;
    public Color[] blockColor;
    public Color greenColor;
    public AudioSource S_GameOver, S_GreenOrb, S_Plus; // Sound의 S
    public AudioSource[] S_Block;
    public Quaternion QI = Quaternion.identity;
    public bool shotTrigger, shotable;
    public Vector3 veryFirstPos; // 최초 공이 충돌할 좌표

    Vector3 firstPos, secondPos, gap;
    int score, timerCount, launchIndex;
    bool timerStart, isDie, isNewRecord, isBlockMoving;
    float timeDelay;

    #region 시작
    private void Awake_GM()
    {
        // 9:16 고정해상도 카메라
        Camera camera = Camera.main; // camera 변수에 메인카메라 넣기
        Rect rect = camera.rect;
        float scaleheight = ((float)Screen.width / Screen.height) / ((float)9 / 16); // (가로 / 세로)
        float scalewidth = 1f / scaleheight;

        if (scaleheight < 1)
        {
            rect.height = scaleheight;
            rect.y = (1f - scaleheight) / 2f;
        }
        else
        {
            rect.width = scalewidth;
            rect.x = (1f / scalewidth) / 2f;
        }
        camera.rect = rect;

        // 시작
        BlockGenerator();
        BestScoreText.text = "최고기록 : " + PlayerPrefs.GetInt("BestScore").ToString(); 
    }

    public void Restart()
        => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // 현재씬 재시작

    public void VeryFirstPosSet(Vector3 pos)
    {
        if (veryFirstPos == Vector3.zero)
            veryFirstPos = pos;
    }

    #endregion

    #region 블럭

    void BlockGenerator()
    {
        // 점수
        ScoreText.text = "현재점수 : " + (++score).ToString();
        if(PlayerPrefs.GetInt("BestScore", 0) < score)
        {
            PlayerPrefs.SetInt("BestScore", score);
            BestScoreText.text = "최고기록 : " + PlayerPrefs.GetInt("BestScore").ToString();
            BestScoreText.color = greenColor;
            isNewRecord = true;
        }

        // 점수에 따른 블럭 복사개수 정하기
        int count;
        int randBlock = Random.Range(0, 24);
        if (score <= 10)
            count = randBlock < 16 ? 1 : 2; // 16/24 확률로 1개, 8/24 확률로 2개
        else if (score <= 20)
            count = randBlock < 8 ? 1 : (randBlock < 16 ? 2 : 3); // 8/24 확률로 1개, 8/24 확률로 2개, 8/24 확률로 3개
        else if (score <= 40)
            count = randBlock < 9 ? 2 : (randBlock < 18 ? 3 : 4); // 9/24 확률로 2개, 9/24 확률로 3개, 6/24 확률로 4개
        else
            count = randBlock < 8 ? 2 : (randBlock < 16 ? 3 : (randBlock < 20 ? 4 : 5)); // 8/24 확률로 2개, 8/24 확률로 3개, 4/24 확률로 4개, 4/24 확률로 5개

        // 스폰좌표에 블럭, 초록구 생성
        List<Vector3> SpawnList = new List<Vector3>();
        for (int i = 0; i < 6; i++)
            SpawnList.Add(new Vector3(-46.7f + i * 18.68f, 51.2f, 0));

        for(int i = 0; i < count; i++)
        {
            int rand = Random.Range(0, SpawnList.Count);

            Transform TR = Instantiate(P_Block, SpawnList[rand], QI).transform;
            TR.SetParent(BlockGroup);
            TR.GetChild(0).GetComponentInChildren<Text>().text = score.ToString();

            SpawnList.RemoveAt(rand);
        }
        Instantiate(P_GreenOrb, SpawnList[Random.Range(0, SpawnList.Count)], QI).transform.SetParent(BlockGroup);

    }

    #endregion

    void Update_GM()
    {
        // 마우스 첫번째 좌표
        if (Input.GetMouseButtonDown(0)) // 마우스를 누를때
            firstPos = Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0, 0, 10);
        // 카메라를 월드포인트로 바꾸고, 기본 카메라는 z값이 -10이기때문에 10을 더해줘서 첫번째 포지션을 잡아준다



        bool isMouse = Input.GetMouseButton(0); // isMouse는 계속 사용될 변수로 눌러진 상태를 의미

        if(isMouse)
        {   // 차이값
            secondPos = Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0, 0, 10);
            if ((secondPos - firstPos).magnitude < 1)
                return;

            gap = (secondPos - firstPos).normalized;
            gap = new Vector3(gap.y >= 0 ? gap.x : gap.x >= 0 ? 1 : -1, Mathf.Clamp(gap.y, 0.2f, 1), 0); // 최소각도 제한


            // 화살표, 공 미리보기
            Arrow.transform.position = veryFirstPos;
            Arrow.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(gap.y, gap.x) * Mathf.Rad2Deg);
            BallPreview.transform.position =
                Physics2D.CircleCast(new Vector2(Mathf.Clamp(veryFirstPos.x, -54, 54), groundY), 1.7f, gap, 10000, 1 << LayerMask.NameToLayer("Wall") | 1 << LayerMask.NameToLayer("Block")).centroid;

            RaycastHit2D hit = Physics2D.Raycast(veryFirstPos, gap, 10000, 1 << LayerMask.NameToLayer("Wall"));

            // 라인
            MouseLR.SetPosition(0, firstPos);
            MouseLR.SetPosition(1, secondPos);
            BallLR.SetPosition(0, veryFirstPos);
            BallLR.SetPosition(1, (Vector3)hit.point - gap * 1.5f);


        }
        BallPreview.SetActive(isMouse);
        Arrow.SetActive(isMouse);

        if (Input.GetMouseButtonUp(0))
        {
            // 라인 초기화
            MouseLR.SetPosition(0, Vector3.zero);
            MouseLR.SetPosition(1, Vector3.zero);
            BallLR.SetPosition(0, Vector3.zero);
            BallLR.SetPosition(1, Vector3.zero);

            timerStart = true;
            veryFirstPos = Vector3.zero;
            firstPos = Vector3.zero;
        }
    }

    private void FixedUpdate_GM()
    {
        // 0.06초 간격으로 공 발사
        if( timerStart && ++ timerCount == 3)
        {
            timerCount = 0;
            BallGroup.GetChild(launchIndex++).GetComponent<OneCommand>().Launch(gap);
            if(launchIndex == BallGroup.childCount)
            {
                timerStart = false;
                launchIndex = 0;
                timerCount = 0;
                BallCountText.text = "";
            }
        }
    }

    #endregion

    #region BallScript.Cs

    [Header("BallScriptValue")]
    public Rigidbody2D RB;
    public bool isMoving;

    OneCommand GM;

    void Start_BALL()
        => GM = GameObject.FindWithTag("GameManager").GetComponent<OneCommand>();
    
         
    public void Launch(Vector3 pos) // 공 발사
    {
        GM.shotTrigger = true;
        isMoving = true;
        RB.AddForce(pos * 7000);
    }


    private IEnumerator OnCollisionEnter2D_BALL(Collision2D col)
    {
        yield return null;
        GameObject Col = col.gameObject;
        Physics2D.IgnoreLayerCollision(2, 2); // 공끼리 충돌을 안함

        //바닥충돌시 최초좌표로 이동
        if (Col.CompareTag("Ground"))
        {
            RB.velocity = Vector2.zero;
            transform.position = new Vector2(col.contacts[0].point.x, GM.groundY);
            GM.VeryFirstPosSet(transform.position);

            while(true)
            {
                yield return null;
                transform.position = Vector3.MoveTowards(transform.position, GM.veryFirstPos, 4);
                if(transform.position == GM.veryFirstPos)
                {
                    isMoving = false;
                    yield break;
                }
            }
        }
        
    }


    #endregion


}
