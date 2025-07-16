using Microsoft.EntityFrameworkCore;
using Zela.Models;

namespace Zela.DbContext;

/// <summary>
/// EF Core DbContext đại diện cho toàn bộ schema database.
/// Chứa các DbSet (table mappings) và cấu hình Fluent API cho quan hệ, khóa chính/khóa ngoại.
/// </summary>
public class ApplicationDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    #region DbSets - Ánh xạ bảng

    // ------------ Users & Authentication ------------
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Status> Statuses { get; set; }
    public DbSet<Friendship> Friendships { get; set; }

    // ------------ Chat Module ------------
    public DbSet<ChatGroup> ChatGroups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Media> Media { get; set; }
    //Sticker
    public DbSet<Sticker> Stickers { get; set; }
    //Reactions
    public DbSet<MessageReaction> MessageReactions { get; set; }

    // ------------ Scheduling & Notifications ------------
    public DbSet<CalendarEvent> CalendarEvents { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<AnalyticsEvent> AnalyticsEvents { get; set; }

    // ------------ VideoCall Module ------------
    public DbSet<VideoRoom> VideoRooms { get; set; }
    public DbSet<RoomParticipant> RoomParticipants { get; set; }
    public DbSet<CallSession> CallSessions { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<CallTranscript> CallTranscripts { get; set; }
    public DbSet<Subtitle> Subtitles { get; set; }
    public DbSet<Recording> Recordings { get; set; }
    
    // ======== NEW VIDEO CALL MODELS ========
    public DbSet<RoomMessage> RoomMessages { get; set; }
    public DbSet<RoomPoll> RoomPolls { get; set; }
    public DbSet<PollOption> PollOptions { get; set; }
    public DbSet<PollVote> PollVotes { get; set; }
    public DbSet<BreakoutRoom> BreakoutRooms { get; set; }
    public DbSet<BreakoutRoomParticipant> BreakoutRoomParticipants { get; set; }
    public DbSet<RoomEvent> RoomEvents { get; set; }

    // ------------ Learning Tools ------------
    public DbSet<Quiz> Quizzes { get; set; }
    public DbSet<QuizQuestion> QuizQuestions { get; set; }
    public DbSet<QuizAttempt> QuizAttempts { get; set; }
    
    public DbSet<QuizAttemptDetail> QuizAttemptDetail { get; set; }

    // ------------ Collaboration & Whiteboard ------------
    public DbSet<WhiteboardSession> WhiteboardSessions { get; set; }
    public DbSet<DrawAction> DrawActions { get; set; }
    public DbSet<WhiteboardTemplate> WhiteboardTemplates { get; set; }

    // ------------ Payment & Subscription ------------
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    public DbSet<FileSummary> FileSummaries { get; set; }

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // -------------------------
        // 1. Composite Keys for Many-to-Many
        // -------------------------

        #region Composite Keys

        // GroupMember và RoomParticipant dùng khóa chính ghép
        modelBuilder.Entity<GroupMember>()
            .HasKey(gm => new { gm.GroupId, gm.UserId });

        modelBuilder.Entity<RoomParticipant>()
            .HasKey(rp => new { rp.RoomId, rp.UserId });

        #endregion

        // -------------------------
        // 2. Users & Authentication Relationships
        // -------------------------

        #region User ↔ Role

        modelBuilder.Entity<Role>()
            .HasOne(r => r.User)
            .WithMany(u => u.Roles)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Xóa cascade Role khi User bị xóa

        #endregion

        #region Friendship Self-References

        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.Requester)
            .WithMany(u => u.SentFriendships)
            .HasForeignKey(f => f.UserId1)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.Addressee)
            .WithMany(u => u.ReceivedFriendships)
            .HasForeignKey(f => f.UserId2)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.Status)
            .WithMany(s => s.Friendships)
            .HasForeignKey(f => f.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        #endregion

        // -------------------------
        // 3. Chat Module Relationships
        // -------------------------

        #region ChatGroup ↔ GroupMember

        modelBuilder.Entity<GroupMember>()
            .HasOne(gm => gm.ChatGroup)
            .WithMany(cg => cg.Members)
            .HasForeignKey(gm => gm.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GroupMember>()
            .HasOne(gm => gm.User)
            .WithMany(u => u.GroupMemberships)
            .HasForeignKey(gm => gm.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        #endregion

        #region ChatGroup ↔ Message & Message ↔ User

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Group)
            .WithMany(g => g.Messages)
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Recipient)
            .WithMany(u => u.ReceivedMessages)
            .HasForeignKey(m => m.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);

        #endregion

        #region Message ↔ Media

        modelBuilder.Entity<Media>()
            .HasOne(md => md.Message)
            .WithMany(m => m.Media)
            .HasForeignKey(md => md.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        #endregion

        // -------------------------
        // 4. Scheduling & Notifications
        // -------------------------

        #region User → CalendarEvent, Notification, AnalyticsEvent

        modelBuilder.Entity<CalendarEvent>()
            .HasOne(ce => ce.User)
            .WithMany(u => u.CalendarEvents)
            .HasForeignKey(ce => ce.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AnalyticsEvent>()
            .HasOne(ae => ae.User)
            .WithMany(u => u.AnalyticsEvents)
            .HasForeignKey(ae => ae.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        #endregion

        // -------------------------
        // 5. VideoCall Module Relationships
        // -------------------------

        #region VideoRoom ↔ RoomParticipant
        
        modelBuilder.Entity<RoomParticipant>()
            .HasOne(rp => rp.VideoRoom)
            .WithMany(vr => vr.Participants)
            .HasForeignKey(rp => rp.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RoomParticipant>()
            .HasOne(rp => rp.User)
            .WithMany(u => u.RoomParticipations)
            .HasForeignKey(rp => rp.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        #endregion
        
        #region VideoRoom unique index

        // Bắt buộc Password (mã tham gia) phải là duy nhất
        modelBuilder.Entity<VideoRoom>()
            .HasIndex(vr => vr.Password)
            .IsUnique();

        #endregion


        #region VideoRoom ↔ CallSession → Attendance & Transcript

        modelBuilder.Entity<CallSession>()
            .HasOne(cs => cs.VideoRoom)
            .WithMany(vr => vr.CallSessions)
            .HasForeignKey(cs => cs.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.CallSession)
            .WithMany(cs => cs.Attendances)
            .HasForeignKey(a => a.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.User)
            .WithMany(u => u.Attendances)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CallTranscript>()
            .HasOne(ct => ct.CallSession)
            .WithMany(cs => cs.Transcripts)
            .HasForeignKey(ct => ct.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region Recording ↔ User

        modelBuilder.Entity<Recording>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Recording ↔ CallSession (Optional relationship)
        modelBuilder.Entity<Recording>()
            .HasOne(r => r.CallSession)
            .WithMany(cs => cs.Recordings)
            .HasForeignKey(r => r.SessionId)
            .OnDelete(DeleteBehavior.SetNull); // When session deleted, keep recording but clear SessionId

        // Indexing for better performance
        modelBuilder.Entity<Recording>()
            .HasIndex(r => r.MeetingCode);
        
        modelBuilder.Entity<Recording>()
            .HasIndex(r => new { r.UserId, r.CreatedAt });

        modelBuilder.Entity<Recording>()
            .HasIndex(r => r.IsActive);
        
        modelBuilder.Entity<Recording>()
            .HasIndex(r => r.SessionId);

        #endregion

        #region Transcript ↔ Subtitle

        modelBuilder.Entity<Subtitle>(entity =>
        {
            entity.HasOne(su => su.CallTranscript)
                .WithMany(ct => ct.Subtitles)
                .HasForeignKey(su => su.TranscriptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(su => su.StartTime)
                .HasPrecision(10, 3);
            entity.Property(su => su.EndTime)
                .HasPrecision(10, 3);
        });

        #endregion

        // -------------------------
        // 6. Learning Tools Relationships
        // -------------------------

        #region Quiz ↔ QuizQuestion & QuizAttempt

        modelBuilder.Entity<QuizQuestion>()
            .HasOne(qq => qq.Quiz)
            .WithMany(q => q.Questions)
            .HasForeignKey(qq => qq.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QuizAttempt>()
            .HasOne(qa => qa.Quiz)
            .WithMany(q => q.Attempts)
            .HasForeignKey(qa => qa.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QuizAttempt>()
            .HasOne(qa => qa.User)
            .WithMany(u => u.QuizAttempts)
            .HasForeignKey(qa => qa.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        #endregion

        // -------------------------
        // 7. Collaboration & Whiteboard
        // -------------------------

        #region WhiteboardSession ↔ DrawAction

        // Xóa cascade khi Session bị xóa
        modelBuilder.Entity<DrawAction>()
            .HasOne(da => da.WhiteboardSession)
            .WithMany(ws => ws.DrawActions)
            .HasForeignKey(da => da.WbSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict khi xóa User, để tránh multiple cascade paths
        modelBuilder.Entity<DrawAction>()
            .HasOne(da => da.User)
            .WithMany(u => u.DrawActions)
            .HasForeignKey(da => da.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        #endregion
        
        // -------------------------
        // 8. Sticker $ Message
        // -------------------------
        
        #region Sticker ↔ Message
        
        modelBuilder.Entity<Sticker>()
            .HasOne(md => md.Message)
            .WithMany(m => m.Sticker)
            .HasForeignKey(md => md.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        
        #endregion
        
        // -------------------------
        // 9. Message Reactions
        // -------------------------
        
        #region Message ↔ MessageReaction
        
        modelBuilder.Entity<MessageReaction>()
            .HasOne(mr => mr.Message)
            .WithMany(m => m.Reactions)
            .HasForeignKey(mr => mr.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MessageReaction>()
            .HasOne(mr => mr.User)
            .WithMany()
            .HasForeignKey(mr => mr.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Đảm bảo mỗi user chỉ có thể reaction một lần cho mỗi message
        modelBuilder.Entity<MessageReaction>()
            .HasIndex(mr => new { mr.MessageId, mr.UserId })
            .IsUnique();
        
        #endregion
        
        // QuizAttemptDetail 
        // Cấu hình tránh multiple cascade paths cho QuizAttemptDetail
        modelBuilder.Entity<QuizAttemptDetail>()
            .HasOne(qad => qad.Question)
            .WithMany()
            .HasForeignKey(qad => qad.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<QuizAttemptDetail>()
            .HasOne(qad => qad.Attempt)
            .WithMany(a => a.Details)
            .HasForeignKey(qad => qad.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        // ======== NEW VIDEO CALL RELATIONSHIPS ========
        
        #region RoomMessage Relationships
        
        modelBuilder.Entity<RoomMessage>()
            .HasOne(rm => rm.Room)
            .WithMany(r => r.Messages)
            .HasForeignKey(rm => rm.RoomId)
            .OnDelete(DeleteBehavior.NoAction);
            
        modelBuilder.Entity<RoomMessage>()
            .HasOne(rm => rm.Sender)
            .WithMany()
            .HasForeignKey(rm => rm.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<RoomMessage>()
            .HasOne(rm => rm.Session)
            .WithMany(s => s.Messages)
            .HasForeignKey(rm => rm.SessionId)
            .OnDelete(DeleteBehavior.NoAction);
            
        modelBuilder.Entity<RoomMessage>()
            .HasOne(rm => rm.Recipient)
            .WithMany()
            .HasForeignKey(rm => rm.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);
            
        #endregion
        
        #region RoomPoll Relationships
        
        modelBuilder.Entity<RoomPoll>()
            .HasOne(rp => rp.Room)
            .WithMany(r => r.Polls)
            .HasForeignKey(rp => rp.RoomId)
            .OnDelete(DeleteBehavior.NoAction);
            
        modelBuilder.Entity<RoomPoll>()
            .HasOne(rp => rp.Creator)
            .WithMany()
            .HasForeignKey(rp => rp.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<RoomPoll>()
            .HasOne(rp => rp.Session)
            .WithMany(s => s.Polls)
            .HasForeignKey(rp => rp.SessionId)
            .OnDelete(DeleteBehavior.NoAction);
            
        #endregion
        
        #region PollOption & PollVote Relationships
        
        modelBuilder.Entity<PollOption>()
            .HasOne(po => po.Poll)
            .WithMany(p => p.Options)
            .HasForeignKey(po => po.PollId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<PollVote>()
            .HasOne(pv => pv.Poll)
            .WithMany(p => p.Votes)
            .HasForeignKey(pv => pv.PollId)
            .OnDelete(DeleteBehavior.NoAction);
            
        modelBuilder.Entity<PollVote>()
            .HasOne(pv => pv.Option)
            .WithMany(o => o.Votes)
            .HasForeignKey(pv => pv.OptionId)
            .OnDelete(DeleteBehavior.NoAction);
            
        modelBuilder.Entity<PollVote>()
            .HasOne(pv => pv.Voter)
            .WithMany()
            .HasForeignKey(pv => pv.VoterId)
            .OnDelete(DeleteBehavior.Restrict);
            
        #endregion
        
        #region BreakoutRoom Relationships
        
        modelBuilder.Entity<BreakoutRoom>()
            .HasOne(br => br.MainRoom)
            .WithMany(r => r.BreakoutRooms)
            .HasForeignKey(br => br.MainRoomPassword)
            .HasPrincipalKey(r => r.Password)
            .OnDelete(DeleteBehavior.NoAction);
            
        modelBuilder.Entity<BreakoutRoom>()
            .HasOne(br => br.Host)
            .WithMany()
            .HasForeignKey(br => br.HostId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<BreakoutRoomParticipant>()
            .HasKey(brp => new { brp.BreakoutRoomId, brp.UserId });
            
        modelBuilder.Entity<BreakoutRoomParticipant>()
            .HasOne(brp => brp.BreakoutRoom)
            .WithMany(br => br.Participants)
            .HasForeignKey(brp => brp.BreakoutRoomId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<BreakoutRoomParticipant>()
            .HasOne(brp => brp.User)
            .WithMany()
            .HasForeignKey(brp => brp.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        #endregion
        
        #region RoomEvent Relationships
        
        modelBuilder.Entity<RoomEvent>()
            .HasOne(re => re.Room)
            .WithMany(r => r.Events)
            .HasForeignKey(re => re.RoomPassword)
            .HasPrincipalKey(r => r.Password)
            .OnDelete(DeleteBehavior.NoAction);
            
        modelBuilder.Entity<RoomEvent>()
            .HasOne(re => re.User)
            .WithMany()
            .HasForeignKey(re => re.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        #endregion
        
        #region CallSession Self-Reference (Parent-Child)
        
        modelBuilder.Entity<CallSession>()
            .HasOne(cs => cs.ParentSession)
            .WithMany(cs => cs.ChildSessions)
            .HasForeignKey(cs => cs.ParentSessionId)
            .OnDelete(DeleteBehavior.Restrict);
            
        #endregion
        
        #region VideoRoom UpdatedBy Relationship
        
        modelBuilder.Entity<VideoRoom>()
            .HasOne(vr => vr.UpdatedByUser)
            .WithMany()
            .HasForeignKey(vr => vr.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict);
            
        #endregion
        
        // ======== INDEXES FOR PERFORMANCE ========
        
        #region RoomMessage Indexes
        
        modelBuilder.Entity<RoomMessage>()
            .HasIndex(rm => rm.RoomId);
            
        modelBuilder.Entity<RoomMessage>()
            .HasIndex(rm => rm.SessionId);
            
        modelBuilder.Entity<RoomMessage>()
            .HasIndex(rm => rm.SenderId);
            
        modelBuilder.Entity<RoomMessage>()
            .HasIndex(rm => rm.SentAt);
            
        modelBuilder.Entity<RoomMessage>()
            .HasIndex(rm => rm.MessageType);
            
        #endregion
        
        #region RoomPoll Indexes
        
        modelBuilder.Entity<RoomPoll>()
            .HasIndex(rp => rp.RoomId);
            
        modelBuilder.Entity<RoomPoll>()
            .HasIndex(rp => rp.SessionId);
            
        modelBuilder.Entity<RoomPoll>()
            .HasIndex(rp => rp.Status);
            
        modelBuilder.Entity<RoomPoll>()
            .HasIndex(rp => rp.CreatedAt);
            
        #endregion
        
        #region PollVote Indexes
        
        modelBuilder.Entity<PollVote>()
            .HasIndex(pv => pv.PollId);
            
        modelBuilder.Entity<PollVote>()
            .HasIndex(pv => pv.VoterId);
            
        modelBuilder.Entity<PollVote>()
            .HasIndex(pv => new { pv.PollId, pv.VoterId })
            .IsUnique(); // Mỗi người chỉ vote 1 lần cho mỗi poll
            
        #endregion
        
        #region BreakoutRoom Indexes
        
        modelBuilder.Entity<BreakoutRoom>()
            .HasIndex(br => br.MainRoomPassword);
            
        modelBuilder.Entity<BreakoutRoom>()
            .HasIndex(br => br.Status);
            
        modelBuilder.Entity<BreakoutRoom>()
            .HasIndex(br => br.HostId);
            
        #endregion
        
        #region RoomEvent Indexes
        
        modelBuilder.Entity<RoomEvent>()
            .HasIndex(re => re.RoomPassword);
            
        modelBuilder.Entity<RoomEvent>()
            .HasIndex(re => re.EventType);
            
        modelBuilder.Entity<RoomEvent>()
            .HasIndex(re => re.UserId);
            
        modelBuilder.Entity<RoomEvent>()
            .HasIndex(re => re.Timestamp);
            
        #endregion
        
        #region RoomParticipant Indexes
        
        modelBuilder.Entity<RoomParticipant>()
            .HasIndex(rp => rp.UserId);
            
        modelBuilder.Entity<RoomParticipant>()
            .HasIndex(rp => rp.Status);
            
        modelBuilder.Entity<RoomParticipant>()
            .HasIndex(rp => rp.JoinedAt);
            
        modelBuilder.Entity<RoomParticipant>()
            .HasIndex(rp => rp.IsModerator);
            
        modelBuilder.Entity<RoomParticipant>()
            .HasIndex(rp => rp.IsHandRaised);
            
        modelBuilder.Entity<RoomParticipant>()
            .HasIndex(rp => rp.IsMutedByHost);
            
        modelBuilder.Entity<RoomParticipant>()
            .HasIndex(rp => rp.LastActivityAt);
            
        #endregion
        
        #region VideoRoom Indexes
        
        modelBuilder.Entity<VideoRoom>()
            .HasIndex(vr => vr.CreatorId);
            
        modelBuilder.Entity<VideoRoom>()
            .HasIndex(vr => vr.IsOpen);
            
        modelBuilder.Entity<VideoRoom>()
            .HasIndex(vr => vr.CreatedAt);
            
        modelBuilder.Entity<VideoRoom>()
            .HasIndex(vr => vr.RoomType);
            
        modelBuilder.Entity<VideoRoom>()
            .HasIndex(vr => vr.ScheduledStartTime);
            
        modelBuilder.Entity<VideoRoom>()
            .HasIndex(vr => vr.IsLocked);
            
        modelBuilder.Entity<VideoRoom>()
            .HasIndex(vr => vr.WaitingRoomEnabled);
            
        #endregion
        
        #region CallSession Indexes
        
        modelBuilder.Entity<CallSession>()
            .HasIndex(cs => cs.RoomId);
            
        modelBuilder.Entity<CallSession>()
            .HasIndex(cs => cs.StartedAt);
            
        modelBuilder.Entity<CallSession>()
            .HasIndex(cs => cs.EndedAt);
            
        modelBuilder.Entity<CallSession>()
            .HasIndex(cs => cs.SessionType);
            
        modelBuilder.Entity<CallSession>()
            .HasIndex(cs => cs.ParentSessionId);
            
        modelBuilder.Entity<CallSession>()
            .HasIndex(cs => cs.CreatedBy);
            
        #endregion
    }
}
