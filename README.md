# tcp/ip

c# 에서 네트워크 소켓 프로그래밍을 할때 특별히 외부 라이브러리를 사용할 필요는 없습니다. 
단순히 소스코드에서 아래와 같이 두개의 라이브러리를 추가합니다.

```csharp
using System;
using System.Net.Sockets;
```

위 두개만 있으면 이더넷 통신을 손쉽게 구현 할 수 있습니다.

이더넷 통신할때 매번 코드를 생성하는게 귀찮으므로, 라이브러리 형식으로 하나 만들어놓기로 합니다.
우선 ****AsyncObject.cs**** 이란 이름으로 클래스 파일하나를 생성합니다.
비쥬얼스튜디오에서 위와 같이 솔루션 탐색기에서 오른쪽 마우스 클릭후 추가=> 새 항목 들어가신 후에 그냥 visual c# 항목에서 "클래스" 선택 후 아래쪽에 이름을 AsyncObject.cs로 하시면 됩니다. 생성 후 namespace항목을 ****main 코드와 꼭 일치**** 해주시고

```csharp
using System;
using System.Net.Sockets;

namespace rev
{
	public class AsyncObject {
		public byte[] Buffer;
		public Socket WorkingSocket;
		public readonly int BufferSize;
	
		public AsyncObject(int bufferSize) {
			BufferSize = bufferSize;
			Buffer = new byte[BufferSize];
		}

		public void ClearBuffer() {
			Array.Clear(Buffer, 0, BufferSize);
		}
	}
}
```

이 코드 자체는 버퍼를 만들어줘서 내용이 깨져서 들어오지 않도록 방비해줍니다. 버퍼를 중간에 생성해서 데이터를 바로 받았을때 깨지지 않도록 해줍니다.
그리고 clearbuffer()함수를 하나 추가해줘서 언제든지 호출시 버퍼를 깨끗히 해줄수 있도록 합니다.
이제 본격적으로 메인코드에서 어떻게 데이터를 주고 받는지 알아봅시다.

## ****c# 서버 프로그래밍****

1. **전역변수 선언.**

```csharp
Socket mainSock;
IPAddress thisAddress;
List<Socket> connectedClients = new List<Socket>();
int port = 9009;
```

소켓 생성 / 아이피주소 / 접속한 클라이언트 리스트 /포트 번호  를 전역변수로 선언해 둔다.
계속 두고두고 쓰기때문에 꼭 전역변수로 설정해놔야 된다.

1. **서버 생성.**

```csharp
private void server()
{
	mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
	IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, port);
	mainSock.Bind(serverEP);
	mainSock.Listen(10);
	mainSock.BeginAccept(AcceptCallback, null);
}
```

소켓을 InterNetwork/ 스트림 타입/ 프로토콜을 ip로 만든다.
서버아이피는 어차피 서버라서 자기 주소로 그냥 하고 포트번호만 자신이 원하는걸로 서버를 bind()함수로 이제 만든다.
그다음, Listen()함수로 이제 클라이언트가 접속할수 있게 대기시킨다.
마지막으로, BeginAccept()함수로 클라이언트 쪽에서 접속을 시도하면 접속허가하면서  AcceptCallback()함수를 실행시키게 된다.
AcceptCallback()함수가 이제 클라이언트쪽에서 접속하게 되면 실제로 수행하는 코드가 담기게 된다.

1. **접속 후 데이터 받기**

```csharp
private void AcceptCallback(IAsyncResult ar)
{
	// 클라이언트의 연결 요청을 수락한다.
	Socket client = mainSock.EndAccept(ar);
	// 또 다른 클라이언트의 연결을 대기한다.
	mainSock.BeginAccept(AcceptCallback, null);
	AsyncObject obj = new AsyncObject(8);
	obj.WorkingSocket = client;
	// 연결된 클라이언트 리스트에 추가해준다.
	connectedClients.Add(client);
	//클라이언트의 데이터를 받는다.
	client.BeginReceive(obj.Buffer, 0, 8, 0, DataReceived, obj);
}
```

이 AcceptCallback()함수에서는 위에 코드에서 상세히 주석을 달았지만. 부가적으로 설명하자면,
연결된 클라이언트들을 관리하고 데이터를 받아서 처리하는데 데이터를 처리할때 비동기식으로 처리를 수행할수있게 한다. 그게 BeginReceive()함수로써 위의 예제에서는 단순히 데이터를 8자리수만 처리하게 해놨다.
많이 처리할려면 숫자를 바꿀것.
이제 본격적으로 데이터가 들어왔을때 처리하는부분은 아래코드 처럼 처리하면 되는데
코드를 보면 알겠지만 다 받고 나서 다시 BeginReceive() ****꼭 다시 호출****해줘야지 다시 받을 수 있다.

```csharp
private void DataReceived(IAsyncResult ar)
{
    AsyncObject obj = (AsyncObject)ar.AsyncState;
    try
    {
        // 데이터 수신을 끝낸다.
        int received = obj.WorkingSocket.EndReceive(ar);
        // 받은 데이터가 없으면(연결 끊어짐) 끝낸다.
        if (received <= 0)
        {
            obj.WorkingSocket.Close();
            return;
        }
    }
    catch
    {
        return;
    }
    try
    {
        string text = Encoding.ASCII.GetString(obj.Buffer);
        // 받은거 처리부분
        obj.ClearBuffer();
        obj.WorkingSocket.BeginReceive(obj.Buffer, 0, 8, 0, DataRecieved, obj);
    }
    catch(Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
}
```

그리고 try{} catch{} 문으로 감싸긴 했지만 메세지를 출력하지 않는데 빈번히 에러가 날수도있어서 계속 매끄럽게 흘러가야지 계속 메세지창 뜨는게 좋지 않기 때문이다 실제로 네트워크 단에서 에러가 나서 클라이언트가 끊어지면 다시 재접속되고 다시 보낼수 있게 되기 때문에 에러를 출력하는게 오히려 안좋다 그래서 디버그용으로만 ex해놓고 실제로는 메세지 박스를 출력을 하지 않는다.
try {} catch{}문을 안쓰게 되면 데이터를 주고 받다가 코드가 다운된다. 꼭 쓰길.
그래야 에러가 나도 다운되지 않는다.
이제 서버쪽에서 데이터를 보낼때는 어떻게 하는지 알아보면,

```csharp
private void sendingMES(string tts)
{
    // 문자열을 ascii 형식의 바이트로 변환한다.
    byte[] bDts = Encoding.ASCII.GetBytes(tts);
    // 연결된 모든 클라이언트에게 전송한다.
    for (int i = connectedClients.Count -1; i >= 0; i--)
    {
        Socket socket = connectedClients[i];
        try { socket.Send(bDts); }
        catch
        {
            // 오류가 발생하면 전송 취소하고 리스트에서 삭제한다.
            // try { socket.Dispose();} catch{}
            connectedClients.RemoveAt(i);
        }
    }
}
```

위에 코드 처럼 보낼 내용을 tts로 받는다 실제 호출은 아래처럼 하면 된다. 물론 그냥 string 변수값이나 텍스트입력창에서 받은값을 그대로 전달해도 된다.

```csharp
sendingMES("hello");
```

위에 코드는 전체 모든 클라이언트에다가 보내게 지금 해놨는데

```csharp
try{
  socket.Send(bDts);
}
catch{
  connectedClients.RemoveAt(i);
}
```

이 부분 위에 if문을 하나 주어서 보내지 않을 클라이언트들은 패스하면 된다.
예를 들면 2번 클라이언트에만 보내려면,

```csharp
if(i==2){
  try{
    socket.Send(bDts);
  }
  catch{
    connectedClients.RemoveAt(i);
  }
}
```

위와 같이 처리하면 되는것이다. if 조건문만 잘 해주면 알아서 원하는 클라이언트들에게 송신할수 있게 처리할 수 있다.
c# 프로그래밍 에서 클라이언트로 구현 하려면 별로 달라지는 건 없다
그냥 접속하고 접속 에러 나거나 중간에 연결이 끊어질때 재접속만 잘되게 하면 끝이다.
그래서 특별히 언급하지 않는다. 간단히 처리가 되므로...
클라이언트 구현을 실제로 궁금하거나 문의사항있으면 댓글로 남기시면 되겠습니다.
이 비동기식 처리는 실제 현장에서 구현된 코드이므로 특별히 에러날게 전혀없다.
24시간 365일 돌아가는 라인에서 쓰고있으므로, 응용만 제대로하면 아무데나 적용이 편하게 할수있을것이다.