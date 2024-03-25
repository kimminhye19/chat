using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;                                                                 // 여기서부터 밑에는 소켓채팅을 위해 추가함
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace ChatClient
{
                                                                                 // 클라이언트의 텍스트박스에 글을 쓰기위한 델리게이트
                                                                                 // 실제 글을 쓰는것은 Form1클래스의 쓰레드가 아닌 다른 스레드인 ChatHandler의 스레드 이기에
                                                                                 // (만약 컨트롤을 만든 쓰레드가 아닌 다른 스레드에서 텍스트박스에 글을 쓴다면 에러발생)
                                                                                 // ChatHandler의 스레드에서 이 델리게이트를 호출하여 서버에서 넘어오는 메시지를 쓴다.
    delegate void SetTextDelegate(string s);

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        TcpClient tcpClient = null;  
        NetworkStream ntwStream = null;                                            // server와 통신하기 위해
        ChatHandler chatHandler = new ChatHandler();                               // 서버와 채팅을 위해 만듬

        private void btnConnect_Click(object sender, EventArgs e)                  // 입장 버튼을 클릭했을때
        {
            if (btnConnect.Text == "입장")
            {
                try
                {
                    tcpClient = new TcpClient();
                    tcpClient.Connect(IPAddress.Parse("127.0.0.1"), 2022);                       // 연결을 위해 ip와 port 번호 넣기
                    ntwStream = tcpClient.GetStream();                                           // 데이터를 보내고 받는데 사용되는 NetworkStream을 반환함
                    chatHandler.Setup(this, ntwStream, this.txtChatMsg);                         // chatHandler의 Setup메소드
                    Thread chatThread = new Thread(new ThreadStart(chatHandler.ChatProcess));    // chatHandler의 ChatProcess메소드
                    chatThread.Start();                                                          // chatHandler의 ChatProcess메소드 실행
                    Message_Snd("<" + txtName.Text + "> 님께서 접속 하셨습니다.", true);
                    btnConnect.Text = "나가기";
                }
                catch (System.Exception Ex)  // 오류 예외처리
                {
                    MessageBox.Show("Server 오류발생 또는 Start 되지 않았거나\n\n" + Ex.Message, "Client");
                }
            }
            else
            {
                Message_Snd("<" + txtName.Text + "> 님께서 접속해제 하셨습니다.", false);
                btnConnect.Text = "입장";
                chatHandler.ChatClose();
                ntwStream.Close();
                tcpClient.Close();
            }
        }

        private void Message_Snd(string lstMessage, Boolean Msg)                                   // 서버로 메세지를 보내는 함수
        {
            try
            {
                //보낼 데이터를 읽어 Default 형식의 바이트 스트림으로 변환 해서 전송
                string dataToSend = lstMessage + "\r\n";
                byte[] data = Encoding.Default.GetBytes(dataToSend);
                ntwStream.Write(data, 0, data.Length);
            }
            catch (Exception Ex)
            {
                if (Msg == true)                                                                   // 입장하다가 오류가 났다면
                {
                    MessageBox.Show("서버가 Start 되지 않았거나\n\n" + Ex.Message, "Client");
                    btnConnect.Text = "입장";
                    chatHandler.ChatClose();
                    ntwStream.Close();
                    tcpClient.Close();
                }
            }
        }

                                                                                                 //다른 스레드인 ChatHandler의 쓰레드에서 호출하는 함수로
                                                                                                 //델리게이트를 통해 채팅 문자열을 텍스트박스에 씀
        public void SetText(string text)
        {
            if (this.txtChatMsg.InvokeRequired)                                                 // 호출자가 컨트롤이 만들어진 스레드와 다른 스레드에 있기 때문에 메서드를 통해
                                                                                                // 컨트롤을 호출하는 경우 해당 호출자가 호출 메서드를 호출해야 하는지를 나타내는 값을 가져온다
            {
                SetTextDelegate d = new SetTextDelegate(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.txtChatMsg.AppendText(text);
            }
        }


        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void txtMsg_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)                                                            // 엔터키를 누르면
            {
                if (btnConnect.Text == "나가기")                                            //서버에 접속이 된 경우에만 메시지를 서버로  보냄
                {
                    Message_Snd("<" + txtName.Text + ">" + txtMsg.Text, true);
                    txtChatMsg.AppendText("<" + txtName.Text + ">"+txtMsg.Text + "\r\n");
                }
                txtMsg.Text = "";
                e.Handled = true;  //이벤트처리중지, KeyUp or Click등
            }
        }
    }

    public class ChatHandler
    {
        private TextBox txtChatMsg;
        private NetworkStream netStream;
        private StreamReader strReader;
        private Form1 form1;

        public void Setup(Form1 form1, NetworkStream netStream, TextBox txtChatMsg)
        {
            this.txtChatMsg = txtChatMsg;
            this.netStream = netStream;
            this.form1 = form1;
            this.strReader = new StreamReader(netStream, Encoding.Default);                      // 한글깨짐방지, 글 읽어내기
        }

        public void ChatClose()                                                                  // 채팅닫는 메소드
        {
            netStream.Close();
            strReader.Close();
        }

        public void ChatProcess() // 실제 채팅하는 메소드
        {
            while (true) // 무한으로 작동 
            {
                try
                {
                    string lstMessage = strReader.ReadLine();                                     // 메시지를 읽어들임
                    if (lstMessage != null && lstMessage != "")                                   // 공백이 아니라면
                    {
                        form1.SetText(lstMessage + "\r\n");                                       //SetText 메서드에서 델리게이트를 이용하여 서버에서 넘어오는 메시지를 쓴다.
                    }
                }
                catch (System.Exception)
                {
                    break;
                }
            }
        }
    }
}

