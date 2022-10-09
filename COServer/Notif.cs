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
            var list = (await GetUsersAsync(date)).ToList();
            if (list.Contains(user))
            {
                return false;
            }
            list.Add(user);
            await Firestore.SetDataAsync(COLLECTION_ID, date.Date.ToKeyString(), date.Hour.ToString(),
                list.Select(i => i.ID));
            return true;
        }

        /// <summary>
        /// �w�肵�������̒ʒm���I���ɂ��Ă��郆�[�U�[�̃��X�g���� user ���폜���܂��B
        /// </summary>
        /// <returns>�f�[�^��ύX�������ǂ����B</returns>
        public static async Task<bool> RemoveUserAsync(DateHour date, SlackUser user)
        {
            var list = (await GetUsersAsync(date)).ToList();
            if (!list.Contains(user))
            {
                return false;
            }
            list.Remove(user);
            await Firestore.SetDataAsync(COLLECTION_ID, date.Date.ToKeyString(), date.Hour.ToString(),
                list.Select(i => i.ID));
            return true;
        }

        /// <summary>
        /// �w�肵�������̒ʒm���I���ɂ��Ă��郆�[�U�[�̃��X�g���폜���܂��B
        /// </summary>
        /// <returns>�f�[�^��ύX�������ǂ����B</returns>
        public static async Task<bool> RemoveUsersAsync(DateHour date)
        {
            var list = await GetUsersAsync(date);
            if (!list.Any())
            {
                return false;
            }
            await Firestore.SetDataAsync(COLLECTION_ID, date.Date.ToKeyString(), date.Hour.ToString(),
                new List<string>());
            return true;
        }

        /// <summary>
        /// ����܂ł̃f�[�^���폜���܂��B
        /// </summary>
        public static async Task CleanAsync()
        {
            var today = int.Parse(DateOnly.FromDateTime(DateTime.Today).ToKeyString());
            await Firestore.RemoveDocumentsAsync(COLLECTION_ID,
                doc => int.Parse(doc.Id) < today);
        }
    }
}