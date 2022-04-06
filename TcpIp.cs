using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;


namespace tcpipTest.controller
{
    class TcpIp
    {
        Socket mainSock;
        IPAddress thisAddress;
        List<Socket> connectedClients = new List<Socket>();
        int port = 9009;

        private void server()
        {
            mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, port);
            mainSock.Bind(serverEP);
            mainSock.Listen(10);
            mainSock.BeginAccept(AcceptCallback, null);
        }
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
                obj.WorkingSocket.BeginReceive(obj.Buffer, 0, 8, 0, DataReceived, obj);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        private void sendingMES(string tts)
        {
            // 문자열을 ascii 형식의 바이트로 변환한다.
            byte[] bDts = Encoding.ASCII.GetBytes(tts);
            // 연결된 모든 클라이언트에게 전송한다.
            for (int i = connectedClients.Count - 1; i >= 0; i--)
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
    }
}
