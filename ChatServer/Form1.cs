using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatServer
{
                                                                                                               //(만약 컨트롤을 만든 윈폼의 UI쓰레드가 아닌 다른 스레드에서 텍스트박스에 글을 쓴다면 에러발생)
    delegate void SetTextDelegate(string s);
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }



        // 여기서 부터!!!!
        TcpListener _tcpLitener;
        bool _open_flag = false;
        Thread _thread_listener;
        Thread _thread_receive;
        NetworkStream _ntstream;
        StreamReader _streader;
        StreamWriter _stwriter;
        TcpClient _client;

        delegate void D_receive(string data);                                                               //델리게이트 다음번에 자세히 설명하겠음.

 
        private void btnStart_Click(object sender, EventArgs e)                                             //서버 오픈 버튼
        {
            try
            {
                if (!_open_flag)                                                                           //현재 접속중인지 아닌지 판단.(첫 접속일 경우 전역변수 선언부에서 false로 선언하였기때문에 접속중이 아님) 
                {
                    _tcpLitener = new TcpListener(IPAddress.Parse("127.0.0.1"), 2022);                     //텍스트 박스 값으로 TcpListener 생성 (int.parse 는 텍스트를 숫자화 하는 메서드)
                    _tcpLitener.Start();                                                                   //TcpListener 시작
                    _open_flag = true;                                                                     //서버를 오픈하였기 때문에 오픈 플래그를 True로 변경
                    _thread_listener = new Thread(listener);                                               //listener메서드 스레드로 생성
                    _thread_listener.Start();                                                              //스레드 시작                
                    tb_recevie_text("서버가 시작되었습니다.\r\n");                                            
                }
            }
            catch (Exception ex)                                                                            //에러    
            {
                _open_flag = false;                                                                         //실패할경우 오픈이 취소되었음으로 플래그를 false로 변경
                MessageBox.Show(ex.ToString());     //오류 보고  
            }
        }
        void listener()                                                                                     //접속 Client를 받아들이는 메소드
        {
            try
            {
                if (_open_flag)                                                                             //현재 오픈중인지 아닌지 판단
                {
                    _client = _tcpLitener.AcceptTcpClient();                                                //Client가 접속할경우 Client변수 생성
                    create_stream();                                                                        //접속한 Client로 create_stream메소드 실행
                }
                else
                {
                    tb_recevie_text("서버가 열리지 않았습니다\r\n");
                }
            }
            catch (Exception ex)                                                                             //에러
            {
                _open_flag = false;                                                                          //실패할경우 오픈이 취소되었음으로 플래그를 false로 변경
                MessageBox.Show(ex.ToString());
            }
        }
        void create_stream()
        {
            try
            {
                _ntstream = _client.GetStream();                                                          //접속한 Client에서 networkstream 추출
                _client.ReceiveTimeout = 500;                                                             //Client의 ReceiveTimeout
                _streader = new StreamReader(_ntstream, Encoding.Default);                                                  //추출한 networkstream으로 streamreader,writer 생성
                _stwriter = new StreamWriter(_ntstream, Encoding.Default);
                _thread_receive = new Thread(receive);                                                    //receive메서드 스레드로 생성
                _thread_receive.Start();                                                                  //스레드 시작     
            }
            catch (Exception ex)                                                                          //에러
            {
                _open_flag = false;                                                                       //실패할경우 접속이 취소되었음으로 플래그를 false로 변경
                MessageBox.Show(ex.ToString());
            }
        }
        void receive()
        {
            try
            {
                while (_open_flag)                                                                         //현재 오픈중인지 아닌지 판단
                {
                    string _receive_data = _streader.ReadLine();                                           //streamreader의 한줄을 읽어들여 string 변수에 저장                              
                    if (_receive_data != null)                                                             //데이터가 null이 아니면
                    {
                        tb_recevie_text(_receive_data);
                    }
                }

            }
            catch (IOException)                                                                           //IO에러 (Timeout에러도 이쪽으로 걸림)                  
            {
                if (_open_flag)                                                                           //현재 접속중인지 아닌지 체크
                {
                    _thread_receive = new Thread(receive);                                                //접속중일 경우 receive메서드를 이용한 스레드 다시생성
                    _thread_receive.Start();
                }                                                                                         //접속중이 아닐경우 자연스럽게 스레드 정지
            }
            catch (Exception ex)                                                                          //그 밖의 에러
            {
                _open_flag = false;

                tb_recevie_text("클라이언트의 연결이 종료되었습니다\r\n다시 서버오픈을 시도합니다.\r\n");
                _tcpLitener.Stop();
                btnStart_Click(null, null);
            }
        }
        void tb_recevie_text(string data)                                                                  //텍스트박스에 텍스트 추가하는 메서드
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new D_receive(tb_recevie_text), data);                                        //델리게이트 관련 코드로 다음번에 간단히 다시 설명하겠습니다.
            }
            else
            {
                if (data != null)                                                                         //data가 null이 아닐경우
                {
                    txtChatMsg.AppendText(data + "\r\n");                                                 //텍스트박스에 데이터+개행문자 추가
                }
            }
        }
        private void txtMsg_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (_open_flag)                                                                                  //현재 접속중인지 아닌지 체크
                {
                    if (e.KeyChar == 13)                                                                         //전송할 내용이 담긴 TextBox가 비었는지 체크
                    {
                        txtChatMsg.AppendText("<System>"+txtMsg.Text + "\r\n");
                        _stwriter.WriteLine("<System>"+txtMsg.Text);                                                       //StreamWriter 버퍼에 텍스트박스 내용 저장
                        _stwriter.Flush();                                                                      //StreamWriter 버퍼 내용을 스트림으로 전달
                        txtMsg.Text = null;
                    }
                }
            }
            catch (Exception ex)                                                                                //에러
            {
                _open_flag = false;                                                                             //접속이 취소되었음으로 플래그를 false로 변경

                MessageBox.Show(ex.ToString());                                                                 //에러 내용 보고

            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)                                   //FormClosing 이벤트
        {
            if (_open_flag)
            {
                if (_ntstream.CanRead)
                {
                    _stwriter.WriteLine(" 연결이 끊어졌습니다. ");                                             //StreamWriter 버퍼에 텍스트박스 내용 저장
                    _stwriter.Flush();
                    _ntstream.Close();
                }
                _open_flag = false;
            }
        }

        



        // 초창기 코드... client에게 메시지를 보낼수 없는 코드...ㅋㅋ

        //TcpListener chatServer = new TcpListener(IPAddress.Parse("127.0.0.1"), 2022);
        //public static ArrayList clientSocketArray = new ArrayList();

        //서버 시작/종료 클릭
        //private void btnStart_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        // 현재 서버가 종료 상태인 경우
        //        if (lblMsg.Tag.ToString() == "Stop")
        //        {
        //            //채팅서버 시작
        //            chatServer.Start();
        //            //계속 떠 있으면서 클라이언트의 연결을 기다리는 쓰레드 생성
        //            //이 스레드가 실행하는 메소드에서 클라이언트 연결을 받고
        //            //생성된 클라이언트 소켓을 clientSocketArray에 담고 새로운 쓰레드를 만들어
        //            //접속된 클라이언트 전용으로 채팅을 한다.
        //            Thread waitThread = new Thread(new ThreadStart(AcceptClient));
        //            waitThread.Start();
        //            lblMsg.Text = "Server 시작됨";
        //            lblMsg.Tag = "Start";
        //            btnStart.Text = "서버 종료";

        //        }
        //        else
        //        {
        //            chatServer.Stop();
        //            foreach (Socket soket in Form1.clientSocketArray)
        //            {
        //                soket.Close();
        //            }
        //            clientSocketArray.Clear();
        //            lblMsg.Text = "Server 중지됨";
        //            lblMsg.Tag = "Stop";
        //            btnStart.Text = "서버 시작";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("서버 시작 오류 :" + ex.Message);
        //    }
        //}


        //윈도우 창을 닫을 때
    //    private void Form1_FormClosed(object sender, FormClosedEventArgs e)
    //    {
    //        Application.Exit();
    //        chatServer.Stop();
    //    }


    //    //무한루프로 떠 있으면서 클라이언트 접속을 기다린다.
    //    private void AcceptClient()
    //    {
    //        Socket socketClient = null;
    //        while (true)
    //        {
    //            //Socket 생성 및 연결 대기
    //            try
    //            {
    //                //연결을 기다리다가 클라이언트가 접속하면 AcceptSocket 메서드가 실행되어
    //                //클라이언트와 상대할 소켓을 리턴 받는다.
    //                socketClient = chatServer.AcceptSocket();
    //                //Chatting을 실행하는 ClientHandler 인스턴스화시키고
    //                //접속한 클라이언트 접속 소켓을 할당
    //                ClientHandler clientHandler = new ClientHandler();
    //                clientHandler.ClientHandler_Setup(this, socketClient, this.txtChatMsg);
    //                //클라이언트를 상대하면서 채팅을 수행하는 스레드 생성 후 시작
    //                Thread thd_ChatProcess = new Thread(new ThreadStart(clientHandler.Chat_Process));
    //                thd_ChatProcess.Start();
    //            }
    //            catch (System.Exception)
    //            {
    //                Form1.clientSocketArray.Remove(socketClient); break;
    //            }
    //        }
    //    }


    //    //텍스트박스에 대화내용을 쓰는 메소드 
    //    public void SetText(string text)
    //    {
    //        //t.InvokeRequired가 True를 반환하면 
    //        // Invoke 메소드 호출을 필요로 하는 상태고 즉 현재 스레드가 UI스레드가 아님
    //        //  이때 Invoke를 시키면 UI스레드가 델리게이트에 설정된 메소드를 실행해준다.
    //        //  False를 반환하면 UI스레드가 좁근하는 경우로 컨트롤에 직접 접근해도 문제가 없는 상태다.
    //        if (this.txtChatMsg.InvokeRequired)
    //        {
    //            SetTextDelegate d = new SetTextDelegate(SetText);                                                          //델리게이트 선언
    //            this.Invoke(d, new object[] { text });                                                                     //델리게이트를 통해 글을 쓴다.
    //                                                                                                                       //이경우 UI스레드를 통해 SetText를 호출함
    //        }
    //        else
    //        {
    //            this.txtChatMsg.AppendText(text);                                                                           //텍스트박스에 글을 씀
    //        }
    //    }
    //}





    //public class ClientHandler
    //{
    //    private TextBox txtChatMsg;
    //    private Socket socketClient;
    //    private NetworkStream netStream;
    //    private StreamReader strReader;
    //    private StreamWriter strWriter;
    //    private Form1 form1;


    //    public void ClientHandler_Setup(Form1 form1, Socket socketClient, TextBox txtChatMsg)
    //    {
    //        this.txtChatMsg = txtChatMsg;                                                                                 //채팅 메시지 출력을 위한 TextBox
    //        this.socketClient = socketClient;                                                                        //클라이언트 접속소켓, 이를 통해 스트림을 만들어 채팅한다.
    //        this.netStream = new NetworkStream(socketClient);
    //        Form1.clientSocketArray.Add(socketClient); //클라이언트 접속소켓을 List에 담음
    //        this.strReader = new StreamReader(netStream, Encoding.Default);  // 한글깨짐방지
    //        this.form1 = form1;
    //    }


    //    public void Chat_Process()
    //    {
    //        while (true)
    //        {
    //            try
    //            {
    //                //문자열을 받음
    //                string lstMessage = strReader.ReadLine();
    //                if (lstMessage != null && lstMessage != "")
    //                {
    //                    //Form1클래스의 SetText메소드를 호출
    //                    //SetText에서는 델리게이트를 통해 TextBox에 글을 쓴다.
    //                    //직접 다른 쓰레드의 TextBox에 값을 쓰면 오류 발생 : Cross-thread operation not valid
    //                    form1.SetText(lstMessage + "\r\n");
    //                    byte[] bytSand_Data = Encoding.Default.GetBytes(lstMessage + "\r\n");
    //                    lock (Form1.clientSocketArray)
    //                    {
    //                        foreach (Socket soket in Form1.clientSocketArray)
    //                        {
    //                            NetworkStream stream = new NetworkStream(soket);
    //                            stream.Write(bytSand_Data, 0, bytSand_Data.Length);
    //                        }
    //                    }
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                MessageBox.Show("채팅 오류 : " + ex.ToString());
    //                Form1.clientSocketArray.Remove(socketClient);
    //                break;
    //            }
    //        }
    //    }
    }
}


