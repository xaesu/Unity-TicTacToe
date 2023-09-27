using System;
using System.Collections;
using System.Collections.Generic;

// 파일 및 입출력 스트림
using System.IO;

// 코드 상호작용 지원
using System.Runtime.InteropServices;

public class Queue
{
    struct Info
    {
        public int offset;   // 데이터 버퍼의 시작 오프셋(오브젝트의 첫 요소)
        public int size;     // 데이터 크기
    };

    private MemoryStream buffer;    // 데이터 저장 스트림
    private List<Info> list;        // 데이터 정보 저장 리스트
    private int offset = 0;         // 다음 데이터가 저장될 오프셋

    private Object lockObj = new Object();      // 스레드 동기화를 위한 락 오브젝트 할당

    public Queue()  // 생성자
    {
        buffer = new MemoryStream();    // 메모리 스트림 생성
        list = new List<Info>();        // 데이터 정보 리스트 생성
    }

    // 데이터를 큐에 추가하는 메서드
    public int Enqueue(byte[] data, int size)
    {
        Info info = new Info();     // 데이터 정보 구조체 생성

        info.offset = offset;       // 현재 오프셋 저장
        info.size = size;           // 데이터 크기 저장

        lock (lockObj)              // 스레드 동기화
        {
            list.Add(info);         //  데이터 정보 추가

            buffer.Position = offset;       // 버퍼 포지션을 현재 오프셋으로 설정
            buffer.Write(data, 0, size);    // 데이터를 버퍼에 작성
            buffer.Flush();                 // 데이터를 실제 메모리에 기록하고 버퍼를 비움
            offset += size;                 // 다음 데이터가 저장될 오프셋 업데이트
        }

        return size;    // 추가한 데이터 크기 반환
    }

    // 데이터를 큐에서 제거하는 메서드
    public int Dequeue(ref byte[] data, int size)
    {
        // 큐에서 대기 중엔 데이터 정보가 없으면
        if (list.Count <= 0)
        {
            return -1;      // 메서드 실행 중지
        }

        int iSize = 0;      // 실제로 읽은 데이터 크기

        lock (lockObj)      // 스레드 동기화 시작
        {
            Info info = list[0];    // 큐의 첫번째 데이터 정보

            int dataSize = Math.Min(size, info.size);       // 데이터 크기 중 가장 작은 값
            buffer.Position = info.offset;                  // 버퍼 포지션을 데이터 시작 오프셋으로 설정
            iSize = buffer.Read(data, 0, dataSize);         // 데이터 읽기

            // 데이터를 성공적으로 읽었으면
            if (iSize > 0)
            {
                list.RemoveAt(0);       // 데이터 큐에서 제거
            }

            // 큐가 비어있으면
            if (list.Count == 0)
            {
                Clear();        // 버퍼 초기화
                offset = 0;     // 오프셋 초기화
            }
        }

        return iSize;   // 실제로 읽은 데이터 크기 반환
    }

    // 버퍼를 비우고 초기화하는 메서드
    public void Clear()
    {
        byte[] data = buffer.GetBuffer();       // 버퍼 데이터를 배열로 호출
        Array.Clear(data, 0, data.Length);      // 데이터 전체 초기화

        buffer.Position = 0;    // 버퍼 포지션 초기화
        buffer.SetLength(0);    // 버퍼 길이 초기화 (데이터 삭제)
    }
}