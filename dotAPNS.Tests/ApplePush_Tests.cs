using System;
using System.Linq;
using Newtonsoft.Json;
using Xunit;

namespace dotAPNS.Tests
{
    public class ApplePush_Tests
    {
        [Fact]
        public void Adding_ContentAvailable_To_Push_With_Badge_or_Sound_Fails()
        {
            var pushWithContentAvailable = ApplePush.CreateContentAvailable();

            Assert.Throws<InvalidOperationException>(() => pushWithContentAvailable.AddBadge(0));
            Assert.Throws<InvalidOperationException>(() => pushWithContentAvailable.AddSound("sound"));
        }

        [Fact]
        public void Adding_Token_and_VoipToken_Together_Fails()
        {
            var pushWithToken = ApplePush.CreateContentAvailable().AddToken("token");
            var pushWithVoipToken = ApplePush.CreateContentAvailable(true).AddVoipToken("voip");
            var alertPushWithToken = ApplePush.CreateAlert(new ApplePushAlert("title", "body")).AddToken("token");

            Assert.Throws<InvalidOperationException>(() => pushWithToken.AddVoipToken("voip"));
            Assert.Throws<InvalidOperationException>(() => pushWithVoipToken.AddToken("token"));
            Assert.Throws<InvalidOperationException>(() => alertPushWithToken.AddVoipToken("voip"));
        }

        [Fact]
        public void Ensure_Type_Correspond_To_Payload()
        {
            var pushWithContentAvailable = ApplePush.CreateContentAvailable();
            var pushWithAlert = ApplePush.CreateAlert(new ApplePushAlert("title", "body"));

            Assert.Equal(ApplePushType.Background, pushWithContentAvailable.Type);
            Assert.Equal(ApplePushType.Alert, pushWithAlert.Type);
        }

        [Fact]
        public void Ensure_Priority_Corresponds_To_Payload()
        {
            var pushWithContentAvailable = ApplePush.CreateContentAvailable();
            var pushWithAlert = ApplePush.CreateAlert(new ApplePushAlert("title", "body"));

            Assert.Equal(5, pushWithContentAvailable.Priority);
            Assert.Equal(10, pushWithAlert.Priority);
        }

        [Fact]
        public void Adding_Voip_To_Alert_Push_Throws_InvalidOperationException()
        {
            var alertPush = ApplePush.CreateAlert(new ApplePushAlert("title", "body"));

            Assert.Throws<InvalidOperationException>(() => alertPush.AddVoipToken("voip"));
        }

        [Fact]
        public void Adding_Voip_To_NonVoip_Type_Throws_InvalidOperationException()
        {
            var backgroundPush = ApplePush.CreateContentAvailable();
            var alert = ApplePush.CreateContentAvailable();

            Assert.Throws<InvalidOperationException>(() => backgroundPush.AddVoipToken("voip"));
        }

        [Fact]
        public void Adding_Token_To_Voip_Type_Throws_InvalidOperationException()
        {
            var backgroundPush = ApplePush.CreateContentAvailable(true);

            Assert.Throws<InvalidOperationException>(() => backgroundPush.AddToken("token"));
        }

        [Fact]
        public void CreateContentAvailable_Has_Background_Type()
        {
            var voipPush = ApplePush.CreateContentAvailable();
            Assert.Equal(ApplePushType.Background, voipPush.Type);
        }

        [Fact]
        public void CreateContentAvailableAsVoip_Has_Voip_Type()
        {
            var voipPush = ApplePush.CreateContentAvailable(true);
            Assert.Equal(ApplePushType.Voip, voipPush.Type);
        }

        [Fact]
        public void AddCustomProperty_Correctly_Adds_String_Value()
        {
            var push = ApplePush
                .CreateAlert("testAlert")
                .AddCustomProperty("customPropertyKey", "customPropertyValue");

            var payload = push.GeneratePayload();
            string payloadJson = JsonConvert.SerializeObject(payload);

            const string referencePayloadJson = "{\"aps\":{\"alert\":\"testAlert\"},\"customPropertyKey\":\"customPropertyValue\"}";
            Assert.Equal(referencePayloadJson, payloadJson);
        }

        [Fact]
        public void AddCustomProperty_Correctly_Adds_Complex_Value()
        {
            var push = ApplePush
                .CreateAlert("testAlert")
                .AddCustomProperty("customPropertyKey", new { value1 = "123", value2 = 456 });

            var payload = push.GeneratePayload();
            string payloadJson = JsonConvert.SerializeObject(payload);

            const string referencePayloadJson = "{\"aps\":{\"alert\":\"testAlert\"},\"customPropertyKey\":{\"value1\":\"123\",\"value2\":456}}";
            Assert.Equal(referencePayloadJson, payloadJson);
        }

        [Fact]
        public void Setting_Custom_Priority()
        {
            var push = ApplePush.CreateContentAvailable();
            Assert.Equal(5, push.Priority);
            push.SetPriority(10);
            Assert.Equal(10, push.Priority);
        }

        [Fact]
        public void AddContentAvailable()
        {
            var push = new ApplePush(ApplePushType.Background);

            push.AddContentAvailable();

            var payload = push.GeneratePayload();
            string payloadJson = JsonConvert.SerializeObject(payload);
            const string referenceJson = "{\"aps\":{\"content-available\":\"1\"}}";
            Assert.Equal(referenceJson, payloadJson);
        }

        [Fact]
        public void Creating_Push_With_ContentAvailable_MutableContent_Alert()
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddContentAvailable()
                .AddMutableContent()
                .AddAlert("title", "body");

            var payload = push.GeneratePayload();
            string payloadJson = JsonConvert.SerializeObject(payload);
            const string referenceJson = "{\"aps\":{\"content-available\":\"1\",\"mutable-content\":\"1\",\"alert\":{\"title\":\"title\",\"body\":\"body\"}}}";
            Assert.Equal(referenceJson, payloadJson);
        }

        [Fact]
        public void Generating_Payload_With_FileProvider_Type_Without_ContainerIdentifier_Fails()
        {
            var push = new ApplePush(ApplePushType.FileProvider);
            Assert.Throws<InvalidOperationException>(() => push.GeneratePayload());
        }

        [Fact]
        public void Adding_ContainerIdentifier_To_All_Types_Except_FileProvider_Fails()
        {
            var types = ((ApplePushType[]) Enum.GetValues(typeof(ApplePushType)))
                .Where(t => t != ApplePushType.FileProvider);

            foreach (var type in types)
            {
                var push = new ApplePush(type);
                Assert.Throws<InvalidOperationException>(() => push.AddContainerIdentifier());
            }
        }

        [Fact]
        public void Generated_Payload_With_FileProvider_Type_Containts_ContainerProvider_Field_Only()
        {
            const string expectedJson = "{\"container-identifier\":\"test-container-identifier\"}";
            var push = new ApplePush(ApplePushType.FileProvider)
                .AddContainerIdentifier("test-container-identifier");

            var payload = push.GeneratePayload();
            string json = JsonConvert.SerializeObject(payload);

            Assert.Equal(expectedJson, json);
        }
    }
}