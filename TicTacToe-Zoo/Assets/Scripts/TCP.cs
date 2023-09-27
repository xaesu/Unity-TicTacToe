using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 네트워크 프로그래밍 관련 네임 스페이스
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class TCP : MonoBehaviour
{
    Socket sServer = null;   // 서버 소켓
    Socket sClient = null;   // 클라이언트 소켓

    Queue qSend;            // 데이터 전송 큐
    Queue qReceive;         // 데이터 수신 큐

    bool bServer = false;       // 서버 모드 여부 플래그
    bool bConnect = false;      // 연결 상태 여부 플래그

    bool bThread = false;       // 백그라운드 스레드 실행 여부 플래그
    Thread thread = null;       // 백그라운드 스레드

    void Start()
    {
        qSend = new Queue();        // 전송 큐 메모리 공간 생성
        qReceive = new Queue();     // 수신 큐 메모리 공간 생성
    }

    public bool IsServer()
    {
        return bServer;
    }

    public bool IsConnect()
    {
        return bConnect;
    }

    public int Send(byte[] data, int size)  // 전송 데이터 배열, 크기
    {
        // 전송 큐 메모리가 생성되지 않았을 경우
        if (qSend == null)
        {
            return 0;
        }

        // 전송 큐 메모리에 데이터 추가하고 추가된 데이터 반환
        return qSend.Enqueue(data, size);
    }

    public int Receive(ref byte[] data, int size)
    {
        if (qReceive == null)
        {
            return 0;
        }

        // 수신 큐 메모리에서 데이터를 꺼내 데이터에 저장하고 꺼낸 데이터의 크기 반환
        return qReceive.Dequeue(ref data, size);
    }

    // 서버 소켓이 바인딩될 포트번호, 클라이언트 연결 대기열의 크기
    public bool StartServer(int port, int backlog)
    {
        // 서버 소켓 생성
        sServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // 서버 소켓을 사용할 포켓에 바인딩 (대기함수)
        sServer.Bind(new IPEndPoint(IPAddress.Any, port));

        // 클라이언트 연결 대기열 크기 설정 - 대기 시작
        sServer.Listen(backlog);

        // 서버 플래그 설정 (서버 실행 중)
        bServer = true;
        Debug.Log("Server Start");

        // 백그라운드 스레드(통신 스레드) 시작 및 성공 여부 반환
        return StartThread();
    }

    public void StopServer()
    {
        // 백그라운드 스레드 종료
        bThread = false;

        // 백그라운드 스레드가 생성되면
        if (thread != null)
        {
            // 스레드 종료까지 대기, 스레드 참조 해제
            thread.Join();
            thread = null;
        }

        // 클라이언트 연결 해제
        Disconnect();

        // 서버 소켓이 열려있을 때 (닫혀있을 때 닫을 필요 없음)
        if (sServer != null)
        {
            // 서버 소켓 종료, 서버 소켓 참조 해제
            sServer.Close();
            sServer = null;
        }

        // 서버 플래그 비활성화 (서버 중지)
        bServer = false;
    }

    // endpoint 매개 변수로 입력 
    public bool Connect(string address, int port)
    {
        // 반환값 저장
        bool ret = false;

        // 클라이언트 소켓 생성, 지정된 서버 주소와 포트에 연결
        sClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        sClient.Connect(address, port);

        // 백그라운드 스레드(통신 스레드) 시작
        ret = StartThread();

        // 백그라운드 스레드가 시작되면
        if (ret == true)
        {
            // 클라이언트 연결 플래그 활성화
            bConnect = true;
            Debug.Log("Server Connnect");
        }

        // 클라이언트 연결 상태 반환
        return bConnect;
    }

    public void Disconnect()
    {
        // 클라이언트 연결 비활성화
        bConnect = false;

        // 클라이언트 소켓이 남아있을 때
        if (sClient != null)
        {
            // 클라이언트 소켓에 양방향 셧다운 (양방향 통신 종료)
            sClient.Shutdown(SocketShutdown.Both);

            // 클라이언트 소켓 종료, 소켓 참조 해제
            sClient.Close();
            sClient = null;
        }
    }

    bool StartThread()
    {
        bThread = true;
        thread = new Thread(new ThreadStart(NetworkUpdate));
        thread.Start();

        return true;
    }

    public void NetworkUpdate()
    {
        while (bThread)
        {
            // 클라이언트 연결 대기
            WaitClient();

            // 클라이언트 소켓이 생성되어있고, 서버가 연결되어 있을 때
            if (sClient != null && bConnect == true)
            {
                UpdateSend();       // 데이터 전송 업데이트
                UpdateReceive();    // 데이터 수신 업데이트
            }

            // 통신 스레드 일시정지
            Thread.Sleep(5);
        }
    }

    void WaitClient()
    {
        // 서버 소켓이 생성되어있고, 서버가 데이터를 읽을 준비가 되어있으면 (Poll)
        if (sServer != null && sServer.Poll(0, SelectMode.SelectRead))
        {
            // 클라이언트 연결 요청을 수락하고 새로운 클라이언트 소켓 생성
            sClient = sServer.Accept();

            // 연결 플래그 활성화
            bConnect = true;
            Debug.Log("Connect Client");
        }
    }

    void UpdateSend()
    {
        if (sClient.Poll(0, SelectMode.SelectWrite))
        {
            // 데이터 저장 배열 생성
            byte[] data = new byte[1024];

            // 전송할 데이터를 큐에서 꺼내 배열에 저장하고 꺼낸 데이터 반환
            int iSize = qSend.Dequeue(ref data, data.Length);

            // 전송할 데이터가 있을 때 반복
            while (iSize > 0)
            {
                // 클라이언트 소켓을 사용해 데이터 전송
                sClient.Send(data, iSize, SocketFlags.None);

                // 다음 전송할 데이터를 큐에서 꺼내 배열에 저장하고 크기 반환
                iSize = qSend.Dequeue(ref data, data.Length);
            }
        }
    }

    void UpdateReceive()
    {
        while (sClient.Poll(0, SelectMode.SelectRead))
        {
            byte[] data = new byte[1024];

            // 클라이언트 소켓을 통해 데이터를 수신하고 수신된 데이터의 크기 반환
            int iSize = sClient.Receive(data, data.Length, SocketFlags.None);

            // 수신된 데이터가 없을 경우 연결 해제
            if (iSize == 0)
            {
                Disconnect();
            }

            else if (iSize > 0)
            {
                // 수신 데이터 큐에 추가
                qReceive.Dequeue(ref data, iSize);
            }
        }
    }
}