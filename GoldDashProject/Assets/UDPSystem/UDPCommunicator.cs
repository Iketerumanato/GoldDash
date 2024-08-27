using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Linq;
using System.Threading.Tasks;
using System;

public class UdpCommunicator
{
    private const int START_PORT = 60000; //�͂��߂Ɏg�p�����݂�|�[�g�ԍ�
    private const int BROADCAST_RANGE = 10; //�u���[�h�L���X�g���M������̃|�[�g�ԍ����킩��Ȃ��̂�START_PORT�ԁ`START_PORT + BROADCAST_RANGE�Ԃ܂ł̃|�[�g�Ɉ�������Ĕ������f��
    private const int WAIT_RESPONSE_TIME = 500; //�u���[�h�L���X�g���M���A�����[�g�R���s���[�^����̃��X�|���X��WAIT_RESPONSE_TIME�~���b�҂��A���X�|���X���Ȃ���ΐV���ȃ|�[�g�Ƀu���[�h�L���X�g���M������
    private const int NUM_OF_RETRY_BROADCAST = 2; //�u���[�h�L���X�g���M���A�����[�g�R���s���[�^����̃��X�|���X���m�F�ł��Ȃ������Ƃ�NUM_OF_RETRY_BROADCAST��đ�����

    private IPEndPoint localEndPointForSend; //�����̑��M�p�G���h�|�C���g
    private IPEndPoint localEndPointForReceive; //�����̎�M�p�G���h�|�C���g�B�ʂɑ��M�p�ƕ����Ȃ��Ă������񂾂��Ǖ�����ƃ|�[�g�̎d���ʂɗ]�T�����܂��

    private HashSet<IPEndPoint> remoteEndPoints; //�ʐM����̃G���h�|�C���g���܂Ƃ߂�n�b�V���Z�b�g�B�N���C�A���g���̏d���΍�ł�����

    private UdpClient sender; //���M�p�N���C�A���g
    private UdpClient receiver; //��M�p�N���C�A���g

    private Queue<byte[]> output; //�O��
    private readonly int numOfRequiredRemoteEndPoints;

    private bool findingRemoteEndPoints; //�ʐM������W���Ȃ�true�A��W��ߐ؂�����false

    UInt16 giveID = 0;

    //�R���X�g���N�^
    public UdpCommunicator(ref Queue<byte[]> output, int numOfRequiredClients)
    {
        //���[�J���R���s���[�^�̃G���h�|�C���g�쐬
        //���[�J���̃G���h�|�C���g�Ƀo�C���h�����N���C�A���g�쐬
        this.localEndPointForSend = new IPEndPoint(GetMyIPAddressIPv4(), GetAvailablePort(START_PORT));
        UnityEngine.Debug.Log($"���M�p���[�J���G���h�|�C���g�𐶐����܂����B�@IP�A�h���X�F{localEndPointForSend.Address}�@�|�[�g�F{localEndPointForSend.Port}");
        this.sender = new UdpClient(localEndPointForSend);
        UnityEngine.Debug.Log("���M�pUDP�N���C�A���g�𐶐����܂����B");

        this.localEndPointForReceive = new IPEndPoint(GetMyIPAddressIPv4(), GetAvailablePort(START_PORT));
        UnityEngine.Debug.Log($"��M�p���[�J���G���h�|�C���g�𐶐��B�@IP�A�h���X�F{localEndPointForReceive.Address}�@�|�[�g�F{localEndPointForReceive.Port}");
        this.receiver = new UdpClient(localEndPointForReceive);
        UnityEngine.Debug.Log("��M�pUDP�N���C�A���g�𐶐����܂����B");

        //�p�P�b�g���o�͐�i�O���N���X�̎��L���[�̎Q�Ɓj���Z�b�g
        this.output = output;
        //�����[�g�G���h�|�C���g�K�v������������
        this.numOfRequiredRemoteEndPoints = numOfRequiredClients;

        //�n�b�V���Z�b�g�쐬�B�܂������
        remoteEndPoints = new HashSet<IPEndPoint>();
        //�ʐM����̕�W�J�n
        StartListen();

        //��M�p�X���b�h�쐬
        Task.Run(() => Receive());
        UnityEngine.Debug.Log("�p�P�b�g��M�p�̔񓯊��������J�n���܂��B");
    }

    //public���\�b�h
    //�p�P�b�g�𑗐M
    public void Send(byte[] sendData)
    {
        //�����[�g�G���h�|�C���g�̃n�b�V���Z�b�g�ɒN���o�^����Ă��Ȃ��Ȃ�LAN���Ƀu���[�h�L���X�g���M����B�N���o�^����Ă���Ȃ炻�̑S���ɑ���B
        if (remoteEndPoints.Count() == 0)
        {
            //�|�[�g�ԍ���ς��Ȃ���u���[�h�L���X�g�@�����|�[�g�ԍ��ɑ΂��Đ��񑗐M���邽��for���̕ω����ɂ͉��������Ă��Ȃ�
            UnityEngine.Debug.Log("�����[�g�G���h�|�C���g�̓o�^���Ȃ����߁A�u���[�h�L���X�g���M���s���܂��B");
            for (int remotePort = START_PORT + 1; remotePort <= START_PORT + BROADCAST_RANGE;)
            {
                UnityEngine.Debug.Log($"{remotePort}�Ԃ̃|�[�g��ΏۂɃu���[�h�L���X�g���M���s���܂��B");
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Broadcast, remotePort);

                //�p�P�b�g���X���l�����āA����g���C����
                for (int retryCount = 0; retryCount <= NUM_OF_RETRY_BROADCAST; retryCount++)
                {
                    sender.Send(sendData, sendData.Length, remoteEndPoint);
                    UnityEngine.Debug.Log($"{sendData.Length}�o�C�g�ȏ�̃p�P�b�g�𑗐M���܂����B");

                    UnityEngine.Debug.Log($"�u���[�h�L���X�g���M�ɑ΂��郌�X�|���X��{WAIT_RESPONSE_TIME}�~���b�҂��܂��E�E�E");
                    Task waitResponse = Task.Delay(WAIT_RESPONSE_TIME);
                    waitResponse.Wait();

                    if (remoteEndPoints.Count != 0)
                    {
                        UnityEngine.Debug.Log($"�����[�g�G���h�|�C���g�̓o�^���m�F����܂����B�u���[�h�L���X�g���M���I�����܂��B");
                        remotePort = START_PORT + BROADCAST_RANGE;
                        break;
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"�����[�g�G���h�|�C���g�̓o�^���m�F�ł��܂���ł����B����{NUM_OF_RETRY_BROADCAST - retryCount}��đ����܂��B");
                    }
                }
                //���̃|�[�g�ԍ���
                remotePort++;
            }
        }
        else
        {
            UnityEngine.Debug.Log("�o�^�ς̃����[�g�G���h�|�C���g�ɑ΂��ăp�P�b�g�𑗐M���܂��B");
            foreach (IPEndPoint ep in remoteEndPoints)
            {
                sender.Send(sendData, sendData.Length, ep);
                UnityEngine.Debug.Log($"{sendData.Length}�o�C�g�ȏ�̃p�P�b�g�𑗐M���܂����B");
            }
        }
    }

    public bool HasRemoteEndPoint()
    { 
        return remoteEndPoints.Count > 0;
    }

    public UInt16 GetReceivePort()
    {
        return (UInt16)this.localEndPointForReceive.Port;
    }

    //�N���C�A���g���ؒf���ꂽ�Ƃ��ɌĂяo���\��̃��\�b�h�B�C�ӂ̃G���h�|�C���g���w�肵�āA�����[�g�G���h�|�C���g�̃n�b�V���Z�b�g���疕������B
    public void RemoveRemoteEndPoint(IPEndPoint ep)
    {
        if (remoteEndPoints.Contains(ep))
        {
            remoteEndPoints.Remove(ep);
            UnityEngine.Debug.Log("1�̃����[�g�G���h�|�C���g�̓o�^���������܂����B");

            //�����[�g�G���h�|�C���g�̕K�v��������Ȃ��Ȃ������W�ĊJ
            if (remoteEndPoints.Count() < numOfRequiredRemoteEndPoints)
            {
                StartListen();
            }
        }
    }

    //private���\�b�h
    //��M�p�X���b�h�Ŏ��s���郁�\�b�h
    private void Receive()
    {
        //�p�P�b�g���M�҂�IPEndPoint�B������IP�A�h���X�A������|�[�g��F�߂�
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (true)
        {
            //�f�o�b�O���O�o��
            UnityEngine.Debug.Log("���b�X����Ԃɓ���܂��B");

            //��M�����f�[�^��ۊ�
            byte[] receivedData = receiver.Receive(ref remoteEndPoint);

            //�������g����̃u���[�h�L���X�g���M�͂����Œe��
            if (remoteEndPoint.Address.Equals(GetMyIPAddressIPv4()))
            {
                UnityEngine.Debug.Log("���[�J���R���s���[�^����̃p�P�b�g����M���܂����B�p�P�b�g��j�����܂��B");
                continue;
            }

            UnityEngine.Debug.Log("�p�P�b�g����M���܂����B�������܂��B");

            //�ʐM����ꗗ�ɓo�^���ꂽ���肩��̃p�P�b�g�Ȃ�G���L���[����A�����łȂ��Ȃ�o�^�����܂��̓p�P�b�g�j��

            if (remoteEndPoints.Contains(remoteEndPoint))
            {
                UnityEngine.Debug.Log("�o�^�σ����[�g�G���h�|�C���g����̃p�P�b�g�ł��B�G���L���[���܂��B");
                output.Enqueue(receivedData);
            }
            else
            {
                if (findingRemoteEndPoints)
                {
                    UnityEngine.Debug.Log("���m�̃����[�g�G���h�|�C���g����̃p�P�b�g�ł��B�p�P�b�g�𐸍����܂��B");
                    RegisterClient(receivedData);
                }
                else
                {
                    UnityEngine.Debug.Log("���m�̃����[�g�G���h�|�C���g����̃p�P�b�g�ł��B���ݐV���ȃ����[�g�G���h�|�C���g���W���Ă��Ȃ����߁A�p�P�b�g��j�����܂��B");
                    continue;
                }
            }
        }

        //�����[�g���n�b�V�����X�g�ɓo�^
        void RegisterClient(byte[] receivedData)
        {
            //receivedData�����Ƀp�P�b�g�̐��������āA�z�肵�Ă��郊���[�g�G���h�|�C���g����̃p�P�b�g�ł���΃G���L���[
            if (CommData.CheckKeyWord(receivedData))
            {
                UnityEngine.Debug.Log("�L�[���[�h�̈�v���m�F�BID��^���A�����[�g�G���h�|�C���g����o�^���܂��B");
                //output.Enqueue(receivedData);


                UnityEngine.Debug.Log("�p�P�b�g���J�����A��M�p�|�[�g�̏����擾���ēo�^���܂��B");
                remoteEndPoints.Add(new IPEndPoint(remoteEndPoint.Address, CommData.GetPort(receivedData)));
                UnityEngine.Debug.Log($"����{remoteEndPoints.Count()}�̃����[�g�G���h�|�C���g���o�^����Ă��܂��B");

                sender.Send(new CommData(giveID, new CommData.POS_DATA[4]).ToByte(), new CommData(giveID, new CommData.POS_DATA[4]).ToByte().Length, new IPEndPoint(remoteEndPoint.Address, CommData.GetPort(receivedData)));
                giveID++;
            }
            else
            {
                UnityEngine.Debug.Log("�L�[���[�h����v���Ȃ����߁A�p�P�b�g��j�����܂��B");
            }
        }
    }

    private void StartListen()
    {
        UnityEngine.Debug.Log($"{numOfRequiredRemoteEndPoints - remoteEndPoints.Count()}�̃����[�g�G���h�|�C���g�̕�W���J�n���܂��B");
        findingRemoteEndPoints = true;
    }

    private void StopLesten()
    {
        UnityEngine.Debug.Log($"{numOfRequiredRemoteEndPoints}�̃����[�g�G���h�|�C���g���o�^���ꂽ���߁A�����[�g�G���h�|�C���g�̕�W���I�����܂��B");
        findingRemoteEndPoints = false;
    }

    //���[�J����IPv4�pIP�A�h���X��Ԃ��B
    private IPAddress GetMyIPAddressIPv4()
    {
        IPAddress ret = null;

        IPAddress[] addrs = Dns.GetHostAddresses(Dns.GetHostName());

        foreach (IPAddress addr in addrs)
        {
            //IPv4�p�A�h���X�ł��邩���ׂ�
            if (addr.AddressFamily.Equals(AddressFamily.InterNetwork))
            {
                //IPv4�p�A�h���X����������ԋp
                ret = addr;
                break;
            }
        }

        return ret;
    }

    //�g�p�\�ȃ|�[�g�ԍ���Ԃ��B�Q�l�Fhttps://note.dokeep.jp/post/csharp-get-active-port/
    private int GetAvailablePort(int startPort)
    {
        //���[�J���̃l�b�g���[�N�ڑ������擾
        IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

        //�A�N�e�B�u��TCP�R�l�N�V�������擾�BIEnum�C���^�[�t�F�[�X�ŕԂ��Ă���̂Ŕz��ɂ���
        IPEndPoint[] tcpConnections = ipGlobalProperties.GetActiveTcpConnections().Select(x => x.LocalEndPoint).ToArray();
        //���ׂĂ�TCP���X�i�[���擾����
        IPEndPoint[] tcpListeners = ipGlobalProperties.GetActiveTcpListeners();
        //���ׂĂ�UDP���X�i�[���擾����
        IPEndPoint[] udpListeners = ipGlobalProperties.GetActiveUdpListeners();

        //Contains�̌v�Z�ʂ����炷���߃��X�g�ł͂Ȃ��n�b�V���Z�b�g�����A��L�̃G���h�|�C���g�z����������Ċi�[����
        //�����Select�֐���IEnum�C���^�[�t�F�[�X��Ԃ��Ă��邪�A�n�b�V���Z�b�g�̃R���X�g���N�^���K���ɏ������Ă����
        HashSet<int> activePorts = new HashSet<int>(
                                                    tcpConnections.
                                                    Concat(tcpListeners).
                                                    Concat(udpListeners). //�����܂Ŕz��̍���
                                                    Where(ipEndPoint => ipEndPoint.Port >= startPort).//startPort�ȍ~�̃G���h�|�C���g���i�荞��
                                                    Select(_ipEndPoint => _ipEndPoint.Port)//�i�荞��IPEndPoint����A�h�b�g���Z�q�Ń|�[�g�ԍ����Q�b�g
                                                    );

        //startPort�Ŏw�肵���ԍ��ȍ~�̃|�[�g�ɂ��āA�g�p�σ|�[�g�̃n�b�V���Z�b�g�Ɋ܂܂�Ȃ��i�ŏ��́j�|�[�g�ԍ���T���ĕԂ�
        for (int port = startPort; port <= 65535; port++)
        {
            if (!activePorts.Contains(port))
                return port;
        }
        //������Ȃ�������-1��Ԃ�
        return -1;
    }
}

/*
�`�������`
ForEach(item => ����)���\�b�h��List<T>�ł�����`����ĂȂ��ĉ}�Ȃ������ɂȂ�܂����B��
���؋L�� https://dasuma20.hatenablog.com/entry/cs/type-of-speed �ɂ��΁AForEach�̏����͂ǂ̃R���N�V�����^�Ŏ��s���Ă��p�t�H�[�}���X����܂�ς��Ȃ��炵���b�X��H
�ĂȂ킯�ŁA���`�̂��Ƃ�IEnumerable<T>���g������ForEach���\�b�h���������Ă��܂����Ǝv���A�������v���Ƃǂ܂�܂����B
�w�g�����\�b�h�͑ΏۂƂ���^�̒񋟎҈ȊO�͍쐬����ׂ��ł͂���܂���xhttps://yone-ken.hatenadiary.org/entry/20090304/p1

public static class EnumerableExtensionMethods
{
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }
}
*/
