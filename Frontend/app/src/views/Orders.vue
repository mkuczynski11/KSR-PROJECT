<template>
  <b-container>
    <b-row>
      <b-col cols="5" />
      <b-col cols="2"> </b-col>
      <b-col cols="5" />
    </b-row>
    <b-row v-if="ordersLoaded" class="py-4">
      <b-col cols="3"></b-col>
      <b-col>
        <b-table
          cols="6"
          v-if="ordersLoaded"
          :fields="fields"
          :items="items"
          show-empty
        >
          <template #cell(id)="data">
            {{ data.item.id }}
          </template>
          <template #cell(bookName)="data">
            {{ data.item.bookName }}
          </template>
          <template #cell(quantity)="data">
            {{ data.item.quantity }}
          </template>
          <template #cell(price)="data">
            {{ data.item.price }}
          </template>
          <template #cell(confirm)="data">
            <b-button
              variant="success"
              @click="openOrderConfirmationForm(data.item)"
              >Potwierdź</b-button
            >
          </template>
          <template #empty="">
            <h4><b>No orders found</b></h4>
          </template>
        </b-table>
      </b-col>
      <b-col cols="3"></b-col>
    </b-row>
    <b-row v-else>
      <b-col cols="12" class="py-4">
        <b-spinner style="width: 3rem; height: 3rem" />
      </b-col>
    </b-row>
    <b-modal id="confirm-order-modal" size="lg" :hide-header="true">
      <b-container>
        <h4>Confirm Your order</h4>
      </b-container>
      <b-row class="pt-2">
        <b-col cols="6">
          <b>Price when ordered</b>
        </b-col>
        <b-col cols="6">
          <b>Current price</b>
        </b-col>
      </b-row>
      <b-row>
        <b-col cols="6" v-if="orderToConfirm">
          {{ orderToConfirm.unitPrice }}
        </b-col>
        <b-col cols="6" v-if="currentPriceLoaded">
          {{ currentPrice }}
        </b-col>
      </b-row>
      <template #modal-footer="{}">
        <b-button variant="danger" @click="closeOrderConfirmationForm()">
          Cancel
        </b-button>
        <b-button
          variant="success"
          @click="confirmOrder()"
          :disabled="!validForm()"
          >Confirm order</b-button
        >
      </template>
    </b-modal>
  </b-container>
</template>

<script>
import { getBook } from "@/api/api.js";

const ORDER_FIELDS = ["ID", "bookName", "quantity", "price", "confirm"];

export default {
  name: "Orders",
  data: function () {
    return {
      orders: [],
      items: [],
      fields: ORDER_FIELDS,
      ordersLoaded: false,
      form: {
        quantity: null,
      },
      orderToConfirm: null,
      currentPrice: null,
      currentPriceLoaded: false,
    };
  },

  mounted() {
    this.parseOrders();
  },

  methods: {
    parseOrders() {
      this.ordersLoaded = false;
      this.orders = this.$orders;
      this.items = [];

      for (let order of this.orders) {
        let parsed_order = {
          id: order.book.id,
          bookName: order.book.name,
          quantity: order.quantity,
          price: order.quantity * 45, // TODO: get price from book
          unitPrice: 45,              // TODO: get price !!!!
        };
        this.items.push(JSON.parse(JSON.stringify(parsed_order)));
      }

      this.ordersLoaded = true;
    },

    async openOrderConfirmationForm(order) {
      this.orderToConfirm = order;
      await this.loadCurrentPrice(order.id);
      this.$bvModal.show("confirm-order-modal");
    },

    closeOrderConfirmationForm() {
      this.$bvModal.hide("confirm-order-modal");
    },

    validForm() {
      // TODO: Add conditions
      return true;
    },

    async loadCurrentPrice(bookId) {
      this.currentPriceLoaded = false;
      // let res = await getBook(bookId);
      // this.currentPrice = res.price;
      this.currentPrice = 45; // TODO: GET CURRENT PRICE
      this.currentPriceLoaded = true;
      console.log(this.currentPrice);
    },

    confirmOrder() {
      // TODO: Add logic about confirming order
      this.$confirmedOrders.push({
        'book': this.bookToReserve,
        'quantity': parseInt(this.form.quantity),
      })

      this.closeOrderConfirmationForm();
    },

    // addToBasket(bookID) {
    //   this.basket.push({
    //     book: book,
    //     quantity: this.form.quantity,
    //   });
    //   this.form.quantity = null;
    //   this.$bvModal.hide("book-reservation-modal");
    // },

    // async viewUser(row) {
    //   await this.loadUser(row.item);
    //   row.toggleDetails();
    // },

    // async loadUser(userObject) {
    //   let { name, surname, email, birthdate } = await getUser(userObject.login);

    //   userObject.name = name;
    //   userObject.surname = surname;
    //   userObject.email = email;
    //   userObject.birthdate = birthdate;
    //   userObject.detailsLoaded = true;
    // },

    // async deleteUser(login) {
    //   await apiDeleteUser(login);
    //   this.parseUsers();
    // },

    // resetCreateForm() {
    //   for (let key in this.form) {
    //     this.form[key] = null;
    //   }
    // },

    // resetEditForm() {
    //   for (let key in this.userToEdit) {
    //     this.form[key] = null;
    //   }
    // },

    // async createUser() {
    //   console.log(this.form);
    //   await apiCreateUser(this.form);
    //   this.resetCreateForm();
    //   this.parseUsers();
    //   this.closeCreateUserForm();
    // },
  },
};
</script>

<style lang="scss" scoped></style>
