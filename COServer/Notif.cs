using COLib;

namespace COServer
{
    /// <summary>
    /// �ǂ̎��Ԃ̒ʒm���ǂ̃��[�U�[���I���ɂ��Ă��邩�̏��������N���X�ł��B
    /// </summary>
    public static class Notif
    {
        /// <summary>
        /// Firestore �R���N�V������ ID �ł��B
        /// </summary>
        private const string COLLECTION_ID = "notif";

        /// <summary>
        /// �w�肵�������̒ʒm���I���ɂ��Ă��郆�[�U�[�̃��X�g��Ԃ��܂��B
        /// </summary>
        public static async Task<IEnumerable<SlackUser>> GetUsersAsync(DateHour date)
        {
            var dict = await Firestore.GetDataAsync(COLLECTION_ID, date.Date.ToKeyString());
            if (dict == null || !dict.ContainsKey(date.Hour.ToString()))
            {
                return new List<SlackUser>();
            }
            return ((List<object>)dict[date.Hour.ToString()]).Select(i => new SlackUser((string)i));
        }

        /// <summary>
        /// �w�肵�������̒ʒm�� user ���I���ɂ��Ă��邩�ǂ�����Ԃ��܂��B
        /// </summary>
        public static async Task<bool> GetValueAsync(DateHour date, SlackUser user)
        {
            return (await GetUsersAsync(date)).Contains(user);
        }

        /// <summary>
        /// �w�肵�������̒ʒm���I���ɂ��Ă��郆�[�U�[�̃��X�g�� user ��ǉ����܂��B
        /// </summary>
        /// <returns>�f�[�^��ύX�������ǂ����B</returns>
        public static async Task<bool> AddUserAsync(DateHour date, SlackUser user)
        {
            return await Firestore.AddDataAsync(COLLECTION_ID, date.Date.ToKeyString(),
                date.Hour.ToString(), user.ID);
        }

        /// <summary>
        /// �w�肵�������̒ʒm���I���ɂ��Ă��郆�[�U�[�̃��X�g���� user ���폜���܂��B
        /// </summary>
        /// <returns>�f�[�^��ύX�������ǂ����B</returns>
        public static async Task<bool> RemoveUserAsync(DateHour date, SlackUser user)
        {
            return await Firestore.RemoveDataAsync(COLLECTION_ID, date.Date.ToKeyString(),
                date.Hour.ToString(), user.ID);
        }

        /// <summary>
        /// ���[�U�[�̎w�肵�������̒ʒm��ݒ肵�܂��B
        /// </summary>
        /// <returns>�f�[�^��ύX�������ǂ����B</returns>
        public static async Task<bool> SetUserAsync(DateHour date, SlackUser user, bool value)
        {
            if (value)
            {
                return await AddUserAsync(date, user);
            }
            else
            {
                return await RemoveUserAsync(date, user);
            }
        }

        /// <summary>
        /// �w�肵�������̒ʒm���I���ɂ��Ă��郆�[�U�[�̃��X�g���폜���܂��B
        /// </summary>
        /// <returns>�f�[�^��ύX�������ǂ����B</returns>
        public static async Task<bool> RemoveUsersAsync(DateHour date)
        {
            return await Firestore.RemoveListAsync(COLLECTION_ID, date.Date.ToKeyString(), date.Hour.ToString());
        }

        /// <summary>
        /// ����܂ł̃f�[�^���폜���܂��B
        /// </summary>
        public static async Task CleanAsync()
        {
            var today = int.Parse(JST.Today.ToKeyString());
            await Firestore.RemoveDocumentsAsync(COLLECTION_ID,
                doc => int.Parse(doc.Id) < today);
        }
    }
}