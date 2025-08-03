import { createSlice, PayloadAction } from "@reduxjs/toolkit";

interface SelectedConversationState {
  conversationId: string | null;
}

const initialState: SelectedConversationState = {
  conversationId: null,
};

const selectedConversationSlice = createSlice({
  name: "selectedConversation",
  initialState,
  reducers: {
    setSelectedConversation(state, action: PayloadAction<string | null>) {
      state.conversationId = action.payload;
    },
    clearSelectedConversation(state) {
      state.conversationId = null;
    },
  },
});

export const { setSelectedConversation, clearSelectedConversation } = selectedConversationSlice.actions;

export default selectedConversationSlice.reducer;
