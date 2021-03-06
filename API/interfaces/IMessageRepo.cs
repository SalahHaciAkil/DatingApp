using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;

namespace API.interfaces
{
    public interface IMessageRepo
    {

        void AddGroup(Group group);
        void RemoveConnection(Connection connection);

        Task<Connection> GetConnection(string coneectionId);
        Task<Group> GetMessageGroup(string groupName);



        void AddMessage(Message message);
        void DeleteMessage(Message message);

        Task<Message> GetMessageAsync(int id);

        Task<PagedList<MessageDto>> GetMessagesForUserAsync(MessageParams messageParams);
        Task<IEnumerable<MessageDto>> GetMessagesThreadAsync(string senderUserName, string recepientUserName);



    }
}