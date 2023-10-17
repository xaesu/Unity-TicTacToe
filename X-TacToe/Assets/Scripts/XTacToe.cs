using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class XTacToe : MonoBehaviour
{
    // 게임 상태 열거
    enum State
    {
        Start = 0,
        Game,
        End,
        Draw,
    }

    // 스톤 열거
    enum Stone
    {
        None = 0,
        Server,
        Client,
    }

    // 차례 열거
    enum Turn
    {
        I = 0,
        You,
    }

    TCP tcp;    // 서버 연결
    public InputField ip;

    [Header("stone")]
    public Texture stoneServer;
    public Texture stoneClient;

    [Header("winner")]
    public Texture winServer;
    public Texture winClient;
    public Texture gameDraw;

    [Header("turn")]
    public Texture turnServer;
    public Texture turnClient;

    [Header("setting")]

    // 틱택토 보드 3*3 배열 선언
    int[][] board = new int[3][];

    State state;        // 게임 상태
    Stone stoneTurn;    // 차례 스톤

    Stone stoneI;       // 본인 Stone
    Stone stoneU;       // 상대 Stone

    Stone stoneWinner;    // 승리자 판정

    int indexRow;      // 배치 위치 행
    int indexCol;      // 배치 위치 열

    void Start()
    {
        tcp = GetComponent<TCP>();
        state = State.Start;

        for (int i = 0; i < 3; i++)
        {
            board[i] = new int[3];

            for (int j = 0; j < 3; j++)
            {
                board[i][j] = (int)Stone.None;
            }
        }
    }

    public void ServerStart()
    {
        tcp.StartServer(10000, 10);
    }

    public void ClientStart()
    {
        tcp.Connect(ip.text, 10000);
    }

    void Update()
    {
        if (!tcp.IsConnect()) return;

        if (state == State.Start)
            UpdateStart();

        else if (state == State.Game)
            UpdateGame();

        else if (state == State.End)
            UpdateEnd();
    }

    void UpdateStart()
    {
        // 게임 모드 상태로 변경
        state = State.Game;

        stoneTurn = Stone.Server;

        // 서버 접속일 때 스톤 세팅
        if (tcp.IsServer())
        {
            stoneI = Stone.Server;
            stoneU = Stone.Client;
        }

        // 클라이언트 접속일 때 스톤 세팅
        else
        {
            stoneI = Stone.Client;
            stoneU = Stone.Server;
        }
    }

    void UpdateGame()
    {
        bool bSet = false;

        // 서버 턴 처리
        if (stoneTurn == stoneI)
            bSet = MyTurn();

        // 클라이언트 턴 처리
        else
            bSet = YourTurn();

        // 
        if (bSet == false)
            return;
    }

    void UpdateEnd()
    {
    }

    bool SetStone(int row, int col, Stone stone)
    {
        // 배치하려는 칸에 스톤이 배치되지 않았을 때
        if (board[row][col] == (int)Stone.None)
        {
            // 칸에 스톤 배치
            board[row][col] = (int)stone;

            indexRow = row;
            indexCol = col;

            return true;
        }

        // 배치하려는 칸에 스톤이 배치되어 있으면 false 리턴
        return false;
    }

    // 틱택토 판에서 배치하려는 위치를 파악
    // 마우스 클릭 시 위치 값에 맞는 board 인덱스 값을 리턴
    int PosToNumber(Vector3 pos)
    {
        float x = (float)(pos.x - 587.5);
        float y = (float)(Screen.height - 237.5 - pos.y);

        // 유효 범위 제한
        if (x < 50.0f || x >= 800.0f) return -1;
        if (y < 50.0f || y >= 800.0f) return -1;

        int h = (int)(x / 248.4f);
        int v = (int)(y / 248.4f);

        int i = v * 3 + h;  // 셀 번호 계산

        return i;
    }

    // 내 턴에서 바둑돌을 배치하는 로직 구현
    bool MyTurn()
    {
        // 클릭 감지
        bool bClick = Input.GetMouseButtonDown(0);
        if (!bClick) return false;

        // 클릭 위치 호출
        Vector3 pos = Input.mousePosition;

        // 클릭 위치 좌표로 변환
        int i = PosToNumber(pos);
        if (i == -1) return false;

        // 좌표 행과 열로 분리
        int row = i / 3;
        int col = i % 3;

        // 스톤 배치
        bool bSet = SetStone(row, col, stoneI);
        if (bSet == false) return false;

        // 위치 좌표 배열로 변환해 전송
        byte[][] data = new byte[1][];
        byte[] sendData = new byte[] { (byte)row, (byte)col };
        tcp.Send(sendData, sendData.Length);

        Debug.Log("보냄: " + row + ", " + col);

        // 승패 확인
        WinnerCheck();

        // 턴 종료
        return true;
    }

    bool YourTurn()
    {
        byte[] receiveData = new byte[2];
        int iSize = tcp.Receive(ref receiveData, receiveData.Length);

        if (iSize <= 0) return false;

        int row = (int)receiveData[0];
        int col = (int)receiveData[1];

        Debug.Log("받음: " + row + ", " + col);

        bool ret = SetStone(row, col, stoneU);
        if (ret == false) return false;

        // 승패 확인
        WinnerCheck();

        return true;
    }

    void WinnerCheck()
    {
        // 승패 확인 후 승자 판정
        stoneWinner = CheckBoard();

        if (stoneWinner != Stone.None)
        {
            // 승자 판정 시 게임 종료
            state = State.End;
            Debug.Log("승리: " + (int)stoneWinner);
        }

        // 무승부 체크
        else
            DrawCheck();

        // 승자가 없으면 턴 넘기기
        stoneTurn = (stoneTurn == Stone.Server) ? Stone.Client : Stone.Server;
    }

    void DrawCheck()
    {
        bool isDraw = true;     // 무승부 상태 디폴트 설정

        // 빈칸 확인
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (board[i][j] == (int)Stone.None)
                {
                    // 빈 칸이 있으면 무승부 상태 해제
                    isDraw = false;
                    break;
                }
            }
        }

        if (isDraw)
        {
            state = State.Draw;
            Debug.Log("Game Draw");
        }
    }

    // 한줄 완성 확인
    Stone CheckBoard()
    {
        int ws = (stoneTurn == Stone.Server) ? (int)Stone.Server : (int)Stone.Client;

        bool matchH = false, matchV = false, matchD = false;

        matchH = Horizontal(indexRow);
        matchV = Vertical(indexCol);
        matchD = Diagonal(indexRow, indexCol);

        if (matchH == true || matchV == true || matchD == true)
            return (Stone)ws;

        // 틱택토 체크 시 일치하는 부분이 없으면 None 리턴
        return Stone.None;
    }

    // 배열 체크 - 가로
    bool Horizontal(int row)
    {
        return ((board[row][0] == board[row][1]) && (board[row][1] == board[row][2]));
    }

    // 배열 체크 - 세로
    bool Vertical(int col)
    {
        return ((board[0][col] == board[1][col]) && (board[1][col] == board[2][col]));
    }

    // 배열 체크 - 대각선
    bool Diagonal(int row, int col)
    {
        if (row == col)
            return ((board[0][0] == board[1][1]) && (board[1][1] == board[2][2]));

        else if (row + col == 2)
            return ((board[0][2] == board[1][1]) && (board[1][1] == board[2][0]));

        else
            return false;
    }

    // 바둑돌을 배치하는 함수
    // 이벤트 발생 시 매 프레임마다 호출 (Update보다 후위)
    private void OnGUI()
    {
        if (!Event.current.type.Equals(EventType.Repaint))
            return;

        // board 값에 따라 바둑돌 출력
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (board[i][j] != (int)Stone.None)
                {
                    float x = 635.0f + j * 250;
                    float y = 280.0f + i * 250;

                    Texture tex = (board[i][j] == (int)Stone.Server) ? stoneServer : stoneClient;
                    Graphics.DrawTexture(new Rect(x, y, 150, 150), tex);
                }
            }
        }

        // 누구의 차례인지 표시
        if (state == State.Game)
        {
            if (tcp != null && tcp.IsServer())
            {
                if (stoneTurn == stoneI)
                    Graphics.DrawTexture(new Rect(195, 600, 220, 160), turnServer);
                else
                    Graphics.DrawTexture(new Rect(1490, 600, 220, 160), turnClient);
            }

            else
            {
                if (stoneTurn == stoneI)
                    Graphics.DrawTexture(new Rect(1490, 600, 220, 160), turnClient);
                else
                    Graphics.DrawTexture(new Rect(195, 600, 220, 160), turnServer);
            }
        }

        // 승리 표시
        if (state == State.End)
        {
            if (tcp != null)
            {
                if (stoneWinner == Stone.Server)
                    Graphics.DrawTexture(new Rect(195, 600, 245, 165), winServer);
                else
                    Graphics.DrawTexture(new Rect(1490, 600, 245, 180), winClient);
            }
        }

        // 무승부 상태
        if (state == State.Draw)
        {
            if (tcp != null)
            {
                Graphics.DrawTexture(new Rect(195, 600, 250, 90), gameDraw);
                Graphics.DrawTexture(new Rect(1490, 600, 250, 90), gameDraw);
            }
        }
    }
}