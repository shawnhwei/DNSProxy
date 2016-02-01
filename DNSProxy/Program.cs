using ARSoft.Tools.Net.Dns;
using System;
using System.Configuration;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DNSProxy
{
    class Program
    {
        static String[] domains = { "netflix.com", "hulu.com", "hbonow.com", "hbogo.com", "amazon.com", "amazon.co.uk", "crackle.com", "pandora.com", "vudu.com", "blinkbox.com", "tvegolf-i.Akamaihd.net", "tvenbcsn-i.Akamaihd.net", "abc.com", "abc.go.com", "fox.com", "link.theplatform.com", "nbc.com", "nbcuni.com", "broadband.espn.go.com", "ip2location.com", "tve_nbc-vh.akamaihd.net", "pbs.org", "warnerbros.com", "southpark.cc.com", "ipad-streaming.cbs.com", "brightcove.com", "cwtv.com", "spike.com", "go.com", "mtv.com", "mtvnservices.com", "video.dl.playstation.net", "uplynk.com", "maxmind.com", "disney.com", "disneyjunior.com", "marketplace-xb.xboxlive.com", "lovefilm.com", "fbchdvod-f.akamaihd.net", "turner.com", "amctv.com", "sho.com", "tveusa-vh.akamaihd.net", "mog.com", "wdtvlive.com", "pga-lh.akamaihd.net", "beinsportsconnect.tv", "beinsportsconnect.net", "besmenabs-s.akamaihd.net", "besmenaas-s.akamaihd.net", "besmenacs-s.akamaihd.net", "besmenads-s.akamaihd.net", "besmenaes-s.akamaihd.net", "besmenafs-s.akamaihd.net", "bbc.co.uk", "bbci.co.uk" };


        static DnsClient netflixClient = new DnsClient(new IPAddress[] { IPAddress.Parse(ConfigurationManager.AppSettings.Get("Proxy")) }, 30000);
        static DnsClient defaultClient = new DnsClient(new IPAddress[] { IPAddress.Parse("8.8.8.8"), IPAddress.Parse("8.8.4.4") }, 30000);

        static void Main(string[] args)
        {
            using (DnsServer server = new DnsServer(IPAddress.Any, 4, 1))
            {
                server.ClientConnected += OnClientConnected;
                server.QueryReceived += OnQueryReceived;

                server.Start();

                Application.Run();

                //Console.WriteLine("Press any key to stop server");
                //Console.ReadLine();
            }

            
        }

        static async Task OnClientConnected(object sender, ClientConnectedEventArgs e)
        {
            if (!IPAddress.IsLoopback(e.RemoteEndpoint.Address))
                e.RefuseConnect = true;
        }

        static async Task OnQueryReceived(object sender, QueryReceivedEventArgs e)
        {
            DnsMessage query = e.Query as DnsMessage;

            if (query == null)
                return;

            DnsMessage response = query.CreateResponseInstance();

            foreach (var question in query.Questions)
            {
                //Console.WriteLine(question.Name.ToString());

                DnsMessage upstreamResponse;
                Boolean useDefault = true;

                foreach (var domain in domains)
                {
                    if (question.Name.ToString().ToLower().Contains(domain))
                    {
                        useDefault = false;
                    }
                }

                if (useDefault)
                {
                    upstreamResponse = await defaultClient.ResolveAsync(question.Name, question.RecordType, question.RecordClass);
                }
                else
                {
                    upstreamResponse = await netflixClient.ResolveAsync(question.Name, question.RecordType, question.RecordClass);
                }

                if (upstreamResponse != null)
                {
                    foreach (DnsRecordBase record in (upstreamResponse.AnswerRecords))
                    {
                        response.AnswerRecords.Add(record);
                    }
                    foreach (DnsRecordBase record in (upstreamResponse.AdditionalRecords))
                    {
                        response.AdditionalRecords.Add(record);
                    }

                    response.ReturnCode = ReturnCode.NoError;

                    e.Response = response;
                }
            }
        }
    }
}
