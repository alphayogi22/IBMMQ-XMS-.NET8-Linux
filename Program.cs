using IBM.WMQ.Nmqi;
using IBM.XMS;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace IBMXMSConsumer
{
    internal class Program
    {
        private const int TIMEOUT_TIME = 30000;

        static void Main(string[] args)
        {

            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => { return true; };


            X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(new X509Certificate2(X509Certificate2.CreateFromCertFile("./keystore/aa_root.crt")));
            store.Close();

            store = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(new X509Certificate2(X509Certificate2.CreateFromCertFile("./keystore/aa_intermediate.crt")));
            store.Close();


            string strQueueManagerName = "QM1";
            string strChannelName = "DEV.APP.SVRCONN";
            string strQueueName = "DEV.QUEUE.1";
            string strServerName = "192.168.1.13";
            string strUserID = "app";
            string strPassword = "passw0rd";
            int intPort = 1414;
            /*string strQueueManagerName = "EMQSCGW1";
            string strChannelName = "UNILODE.01";
            string strQueueName = "CGOOPSHDLG.UNILODE.OPSDATA.01";
            string strServerName = "mqiptstage.aa.com";
            string strUserID = "";
            string strPassword = "";
            int intPort = 1442;*/

            var factoryFactory = XMSFactoryFactory.GetInstance(XMSC.CT_WMQ);
            var cf = factoryFactory.CreateConnectionFactory();

            cf.SetStringProperty(XMSC.WMQ_HOST_NAME, strServerName);
            cf.SetStringProperty(XMSC.USERID, strUserID);
            cf.SetStringProperty(XMSC.PASSWORD, strPassword);
            cf.SetIntProperty(XMSC.WMQ_PORT, intPort);
            cf.SetStringProperty(XMSC.WMQ_CHANNEL, strChannelName);
            cf.SetIntProperty(XMSC.WMQ_CONNECTION_MODE, XMSC.WMQ_CM_CLIENT);
            cf.SetIntProperty(XMSC.WMQ_CLIENT_RECONNECT_OPTIONS, XMSC.WMQ_CLIENT_RECONNECT_Q_MGR);
            cf.SetStringProperty(XMSC.WMQ_QUEUE_MANAGER, strQueueManagerName);
            //cf.SetStringProperty(XMSC.WMQ_SSL_CIPHER_SPEC, "TLS_RSA_WITH_AES_128_CBC_SHA256");

            var tokenSource = new CancellationTokenSource();
            using (var connectionWMQ = cf.CreateConnection())
            {
                using (var sessionWMQ = connectionWMQ.CreateSession(true, AcknowledgeMode.AutoAcknowledge))
                {
                    var cnt = 0;

                    var msg = sessionWMQ.CreateMessage();

                    using (var destination = sessionWMQ.CreateQueue(strQueueName))
                    {
                        using (var consumer = sessionWMQ.CreateConsumer(destination))
                        {
                            connectionWMQ.Start();

                            Console.WriteLine("Producer: Press any key to stop...");
                            _ = Task.Run(() =>
                            {
                                while (!tokenSource.Token.IsCancellationRequested)
                                {
                                    cnt++;
                                    if ((cnt % 1000) == 0)
                                    {
                                        Console.WriteLine($"{cnt} messages received.");
                                        sessionWMQ.Commit();                                        
                                    }

                                    // Wait for 30 seconds for messages. Exit if no message by then
                                    var textMessage = (ITextMessage)consumer.Receive(TIMEOUT_TIME);
                                    if (textMessage == null)
                                        Console.WriteLine("Wait timed out.");
                                }
                            });
                            Thread.Sleep(20000);
                            tokenSource.Cancel();
                        }
                    }
                }
            }
        }
    }
}
