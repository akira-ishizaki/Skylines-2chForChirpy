using System;

namespace Client2ch
{
    public class Message : MessageBase
    {
        private string m_author;
        private uint m_citizenId;
        private string m_text;

        [NonSerialized]
        private string m_URL;

        public Message(string author, string text, string URL)
        {
            m_author = author;
            m_text = text;
            m_URL = URL;
        }

        public override uint GetSenderID()
        {
            return m_citizenId;
        }

        public override string GetSenderName()
        {
            return m_author;
        }

        public override string GetText()
        {
            return m_text;
        }

        /// <summary>
        /// We basically want to ensure the same messages aren't shown twice.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override bool IsSimilarMessage(MessageBase other)
        {
            if (other == null || !(other is Message)) return false;
            var m = other as Message;
            return m != null && (m.m_author == m_author && m.m_text == m_text && m.m_URL == m_URL);
        }

        public override void Serialize(ColossalFramework.IO.DataSerializer s)
        {
            s.WriteSharedString(m_author);
            s.WriteSharedString(m_text);
            s.WriteUInt32(m_citizenId);
        }

        public override void Deserialize(ColossalFramework.IO.DataSerializer s)
        {
            m_author = s.ReadSharedString();
            m_text = s.ReadSharedString();
            m_citizenId = s.ReadUInt32();
        }

        public override void AfterDeserialize(ColossalFramework.IO.DataSerializer s)
        {
        }

        public string GetURL()
        {
            return m_URL;
        }
    }
}
